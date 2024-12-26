//using System;
//using System.Collections.Generic;
//using System.Data.SqlClient;
//using System.IO;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Threading.Tasks;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        string connectionString = "Server=localhost\\SQLEXPRESS;Database=StocksDB;Trusted_Connection=True;";
//        string filePath = "C:/Users/stepa/Documents/code/ilab10/lab10/tickers.txt"; // Путь к файлу с тикерами
//        string apiKey = "c1c0cnRUeG1Vb2hyVVpaVENzZEhGdGgxbl9JWEVFVTJTYmxuc05iNDdEWT0";
//        string baseUrl = "https://api.marketdata.app/v1/stocks/candles/D/{0}/?from={1}&to={2}";

//        // Шаг 1: Загрузка тикеров в базу данных
//        await LoadTickersToDatabase(connectionString, filePath);

//        // Шаг 2: Загрузка данных цен и обновление базы
//        await UpdatePricesAndConditions(connectionString, baseUrl, apiKey);
//    }

//    static async Task LoadTickersToDatabase(string connectionString, string filePath)
//    {
//        using (SqlConnection connection = new SqlConnection(connectionString))
//        {
//            connection.Open();
//            foreach (var line in File.ReadAllLines(filePath))
//            {
//                string ticker = line.Trim();
//                if (!string.IsNullOrEmpty(ticker))
//                {
//                    // Проверка, существует ли уже тикер в базе данных
//                    string checkQuery = "SELECT COUNT(*) FROM Tickers WHERE ticker = @ticker";
//                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
//                    {
//                        checkCommand.Parameters.AddWithValue("@ticker", ticker);
//                        int count = (int)checkCommand.ExecuteScalar();

//                        // Если тикер еще не существует, добавляем его
//                        if (count == 0)
//                        {
//                            string query = "INSERT INTO Tickers (ticker) VALUES (@ticker)";
//                            using (SqlCommand command = new SqlCommand(query, connection))
//                            {
//                                command.Parameters.AddWithValue("@ticker", ticker);
//                                try
//                                {
//                                    command.ExecuteNonQuery();
//                                    Console.WriteLine($"Тикер {ticker} добавлен в базу.");
//                                }
//                                catch (Exception ex)
//                                {
//                                    Console.WriteLine($"Ошибка добавления тикера {ticker}: {ex.Message}");
//                                }
//                            }
//                        }
//                        /*else
//                        {
//                            Console.WriteLine($"Тикер {ticker} уже существует в базе.");
//                        }*/
//                    }
//                }
//            }
//        }

//        Console.WriteLine("Тикеры успешно загружены в базу данных.");
//    }

//    static async Task UpdatePricesAndConditions(string connectionString, string baseUrl, string apiKey)
//    {
//        using (SqlConnection connection = new SqlConnection(connectionString))
//        {
//            connection.Open();
//            SqlCommand getTickersCommand = new SqlCommand("SELECT id, ticker FROM Tickers", connection);
//            SqlDataReader reader = getTickersCommand.ExecuteReader();

//            List<(int id, string ticker)> tickers = new List<(int, string)>();
//            while (reader.Read())
//            {
//                tickers.Add((reader.GetInt32(0), reader.GetString(1)));
//            }
//            reader.Close();

//            HttpClient client = new HttpClient();
//            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

//            foreach (var (tickerId, ticker) in tickers)
//            {
//                DateTime today = DateTime.Now.Date;
//                DateTime yesterday = today.AddDays(-3);

//                string url = string.Format(baseUrl, ticker, yesterday.ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd")); //yyyy-MM-dd
//                try
//                {
//                    var response = await client.GetAsync(url);
//                    if (response.IsSuccessStatusCode) // Проверка успешности ответа
//                    {
//                        var data = await response.Content.ReadFromJsonAsync<StockData>();
//                        if (data != null && data.c.Length >= 2) // c содержит цены
//                        {
//                            decimal yesterdayPrice = data.c[0];
//                            decimal todayPrice = data.c[1];

//                            // Вставка данных в Prices
//                            string insertPriceQuery = "INSERT INTO Prices (tickerId, price, date) VALUES (@tickerId, @price, @date)";
//                            using (SqlCommand command = new SqlCommand(insertPriceQuery, connection))
//                            {
//                                command.Parameters.AddWithValue("@tickerId", tickerId);
//                                command.Parameters.AddWithValue("@price", todayPrice);
//                                command.Parameters.AddWithValue("@date", today);
//                                command.ExecuteNonQuery();
//                            }

//                            // Обновление состояния
//                            string state = todayPrice > yesterdayPrice ? "выросла" : "упала";
//                            string insertConditionQuery = "INSERT INTO TodaysCondition (tickerId, state) VALUES (@tickerId, @state)";
//                            using (SqlCommand command = new SqlCommand(insertConditionQuery, connection))
//                            {
//                                command.Parameters.AddWithValue("@tickerId", tickerId);
//                                command.Parameters.AddWithValue("@state", state);
//                                command.ExecuteNonQuery();
//                            }

//                            Console.WriteLine($"Тикер {ticker}: цена {todayPrice}, состояние {state}");
//                        }
//                        else
//                        {
//                            Console.WriteLine($"Для тикера {ticker} нет данных за указанный период.");
//                        }
//                    }
//                    else
//                    {
//                        Console.WriteLine($"Ошибка при запросе данных для тикера {ticker}: {response.StatusCode}");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Ошибка для {ticker}: {ex.Message}");
//                }
//            }
//        }
//    }
//}

//// Класс для десериализации данных из API
//public class StockData
//{
//    public decimal[] c { get; set; } // Closing prices
//}