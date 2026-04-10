using System;
using System.Data.SqlClient;

namespace NewAtlants_Concrete_Console
{
    class Program
    {
        static string connStr = "Server=localhost;Database=NewAtlants_DB;Trusted_Connection=True;TrustServerCertificate=True;";

        static void Main(string[] args)
        {
            Console.Title = "ООО Новые Атланты - Расчёт бетона";
            Console.ForegroundColor = ConsoleColor.Green;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔════════════════════════════════════════════════╗");
                Console.WriteLine("║   ООО НОВЫЕ АТЛАНТЫ                           ║");
                Console.WriteLine("║   Расчёт бетона                               ║");
                Console.WriteLine("╚════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("1 - Показать все поставки");
                Console.WriteLine("2 - Добавить поставку");
                Console.WriteLine("3 - Удалить поставку");
                Console.WriteLine("4 - Итоги по стройкам");
                Console.WriteLine("0 - Выход");
                Console.WriteLine();
                Console.Write("Выберите действие: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": ShowAll(); break;
                    case "2": AddSupply(); break;
                    case "3": DeleteSupply(); break;
                    case "4": ShowSummary(); break;
                    case "0": return;
                    default: Console.WriteLine("Неверно!"); Console.ReadKey(); break;
                }
            }
        }

        static void ShowAll()
        {
            Console.Clear();
            Console.WriteLine("=== ВСЕ ПОСТАВКИ БЕТОНА ===\n");

            string sql = @"
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY п.Код) AS [№],
                    с.Адрес AS [Стройка],
                    м.Наименование AS [Марка],
                    п.Объем AS [Объем],
                    п.ДатаПоставки AS [Дата],
                    п.ИтоговаяСумма AS [Сумма],
                    CASE WHEN п.Оплачено = 1 THEN 'Да' ELSE 'Нет' END AS [Оплачено]
                FROM ПоставкиБетона п
                JOIN Стройки с ON п.СтройкаКод = с.Код
                JOIN МаркиБетона м ON п.МаркаКод = м.Код
                ORDER BY п.ДатаПоставки DESC";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader r = cmd.ExecuteReader();

                Console.WriteLine($"{"№",-5} {"Стройка",-25} {"Марка",-10} {"Объем",-8} {"Дата",-12} {"Сумма",-12} {"Оплачено"}");
                Console.WriteLine(new string('-', 85));

                int i = 1;
                while (r.Read())
                {
                    Console.WriteLine($"{i++,-5} {r["Стройка"],-25} {r["Марка"],-10} {r["Объем"],-8} {Convert.ToDateTime(r["Дата"]):dd.MM.yyyy,-12} {Convert.ToDecimal(r["Сумма"]):N0,-12} {r["Оплачено"]}");
                }
                r.Close();
            }
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        static void AddSupply()
        {
            Console.Clear();
            Console.WriteLine("=== ДОБАВЛЕНИЕ ПОСТАВКИ ===\n");

            // Показать стройки
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Код, Адрес FROM Стройки", conn);
                SqlDataReader r = cmd.ExecuteReader();
                Console.WriteLine("Стройки:");
                while (r.Read())
                {
                    Console.WriteLine($"{r["Код"]} - {r["Адрес"]}");
                }
                r.Close();
            }

            Console.Write("\nВведите код стройки: ");
            int buildId = Convert.ToInt32(Console.ReadLine());

            // Показать марки
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Код, Наименование, ЦенаЗаКуб FROM МаркиБетона", conn);
                SqlDataReader r = cmd.ExecuteReader();
                Console.WriteLine("\nМарки бетона:");
                while (r.Read())
                {
                    Console.WriteLine($"{r["Код"]} - {r["Наименование"]} ({r["ЦенаЗаКуб"]} руб/куб)");
                }
                r.Close();
            }

            Console.Write("\nВведите код марки: ");
            int markId = Convert.ToInt32(Console.ReadLine());
            Console.Write("Введите объём (куб): ");
            double volume = Convert.ToDouble(Console.ReadLine());
            Console.Write("Введите дату (ГГГГ-ММ-ДД): ");
            DateTime date = DateTime.Parse(Console.ReadLine());

            // Получить цену
            decimal price = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand($"SELECT ЦенаЗаКуб FROM МаркиБетона WHERE Код = {markId}", conn);
                price = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            decimal total = price * (decimal)volume;

            string sql = @"INSERT INTO ПоставкиБетона (СтройкаКод, МаркаКод, Объем, ДатаПоставки, ИтоговаяСумма, Оплачено) 
                           VALUES (@b, @m, @v, @d, @t, 0)";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@b", buildId);
                cmd.Parameters.AddWithValue("@m", markId);
                cmd.Parameters.AddWithValue("@v", volume);
                cmd.Parameters.AddWithValue("@d", date);
                cmd.Parameters.AddWithValue("@t", total);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("\n✅ Поставка добавлена!");
            Console.ReadKey();
        }

        static void DeleteSupply()
        {
            Console.Clear();
            Console.WriteLine("=== УДАЛЕНИЕ ПОСТАВКИ ===\n");

            string sql = @"
                SELECT п.Код, с.Адрес, м.Наименование, п.Объем, п.ДатаПоставки
                FROM ПоставкиБетона п
                JOIN Стройки с ON п.СтройкаКод = с.Код
                JOIN МаркиБетона м ON п.МаркаКод = м.Код";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader r = cmd.ExecuteReader();

                Console.WriteLine($"{"Код",-5} {"Стройка",-25} {"Марка",-10} {"Объем",-8} {"Дата"}");
                Console.WriteLine(new string('-', 60));

                while (r.Read())
                {
                    Console.WriteLine($"{r["Код"],-5} {r["Адрес"],-25} {r["Наименование"],-10} {r["Объем"],-8} {Convert.ToDateTime(r["ДатаПоставки"]):dd.MM.yyyy}");
                }
                r.Close();
            }

            Console.Write("\nВведите код поставки для удаления: ");
            int id = Convert.ToInt32(Console.ReadLine());

            if (MessageBoxConfirm("Удалить поставку?"))
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand($"DELETE FROM ПоставкиБетона WHERE Код = {id}", conn);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine("\n✅ Удалено!");
                Console.ReadKey();
            }
        }

        static void ShowSummary()
        {
            Console.Clear();
            Console.WriteLine("=== ИТОГИ ПО СТРОЙКАМ ===\n");

            string sql = @"
                SELECT 
                    с.Адрес AS [Стройка],
                    SUM(п.Объем) AS [Всего кубов],
                    SUM(п.ИтоговаяСумма) AS [Общая сумма],
                    SUM(CASE WHEN п.Оплачено = 1 THEN п.ИтоговаяСумма ELSE 0 END) AS [Оплачено]
                FROM ПоставкиБетона п
                JOIN Стройки с ON п.СтройкаКод = с.Код
                GROUP BY с.Адрес
                ORDER BY [Всего кубов] DESC";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader r = cmd.ExecuteReader();

                Console.WriteLine($"{"Стройка",-30} {"Кубов",-12} {"Общая сумма",-15} {"Оплачено"}");
                Console.WriteLine(new string('-', 70));

                while (r.Read())
                {
                    Console.WriteLine($"{r["Стройка"],-30} {r["Всего кубов"],-12} {Convert.ToDecimal(r["Общая сумма"]):N0,-15} {Convert.ToDecimal(r["Оплачено"]):N0}");
                }
                r.Close();
            }
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        static bool MessageBoxConfirm(string message)
        {
            Console.Write($"{message} (y/n): ");
            return Console.ReadLine()?.ToLower() == "y";
        }
    }
}