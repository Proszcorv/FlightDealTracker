using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace FlightDealTracker
{
    public class FlightDealTracker
    {
        private readonly ILogger _logger;
        private static readonly HttpClient client = new HttpClient();

        private static readonly string stateFilePath = Path.Combine(Path.GetTempPath(), "lowest_dublin_price.txt");

        public FlightDealTracker(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FlightDealTracker>();
        }

        [Function("FlightDealTracker")]
        public async Task Run([TimerTrigger("0 0 8 */2 * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"A járatkereső elindult: {DateTime.Now}");
            string apiUrl = "https://sky-scrapper.p.rapidapi.com/api/v2/flights/searchFlights" +
                "?originSkyId=BUD&destinationSkyId=DUB" +
                "&originEntityId=95673439&destinationEntityId=95673529" +
                "&date=2027-07-12&returnDate=2027-07-18" +
                "&cabinClass=economy&adults=1" +
                "&currency=HUF&market=hu-HU&countryCode=HUN";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(apiUrl),
                Headers =
                {
                    { "x-rapidapi-key", "ce18f1d14fmshde8408d3296d9c4p10f91bjsn863f186d923d" },
                    { "x-rapidapi-host", "sky-scrapper.p.rapidapi.com" },
                },
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var jsonBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonBody);

                    if (doc.RootElement.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("itineraries", out var itineraries) &&
                        itineraries.GetArrayLength() > 0)
                    {
                        int currentLowestPrice = int.MaxValue;
                        string formattedLowestPrice = "";

                        foreach (var flight in itineraries.EnumerateArray())
                        {
                            if (flight.TryGetProperty("price", out var priceElement) &&
                                flight.TryGetProperty("raw", out var rawPriceElement))
                            {
                                int tempPrice = (int)Math.Round(rawPriceElement.GetDouble());

                                if (tempPrice < currentLowestPrice)
                                {
                                    currentLowestPrice = tempPrice;
                                    formattedLowestPrice = priceElement.GetProperty("formatted").GetString() ?? $"{currentLowestPrice} Ft";
                                }
                            }
                        }

                        if (currentLowestPrice < int.MaxValue)
                        {
                            _logger.LogInformation($"Aktuális legolcsóbb ár Dublinba: {formattedLowestPrice}");

                            int previousLowestPrice = GetPreviousLowestPrice();

                            if (currentLowestPrice < previousLowestPrice)
                            {
                                _logger.LogInformation($"ÚJ REKORD! A régi ár {previousLowestPrice} Ft volt, a mostani {currentLowestPrice} Ft.");

                                await SendEmailNotificationAsync(currentLowestPrice, formattedLowestPrice, previousLowestPrice);
                                SaveNewLowestPrice(currentLowestPrice);
                            }
                            else
                            {
                                _logger.LogInformation($"Nem csökkent az ár. (Eddigi minimum: {previousLowestPrice} Ft). Nem küldünk e-mailt.");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Nem kaptunk járatadatokat. Az API nyers válasza: {jsonBody}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Hiba történt a futás során: {ex.Message}");
            }
        }

        private async Task SendEmailNotificationAsync(int newPrice, string formattedPrice, int oldPrice)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("only.ride.info@gmail.com", "fwbg hqpp ydbp plbv"),
                EnableSsl = true,
            };

            string bookingLink = "https://www.skyscanner.hu/transport/flights/bud/dub/270712/270718/";
            string oldPriceText = oldPrice == int.MaxValue ? "Első mérés" : $"{oldPrice} Ft";

            var mailMessage = new MailMessage
            {
                From = new MailAddress("only.ride.info@gmail.com"),
                Subject = $"ÁRCSÖKKENÉS! Repjegy Dublinba: {formattedPrice}",
                Body = $"Szuper hír!\n\nOlcsóbb lett a repülőjegy Dublinba (2027. július 12-18.)!\n\n" +
                       $"Új ár: {formattedPrice}\n" +
                       $"Korábbi legolcsóbb ár: {oldPriceText}\n\n" +
                       $"Itt tudod megnézni és foglalni:\n{bookingLink}\n\n" +
                       $"Üdv,\nA Járatfigyelőd",
            };

            mailMessage.To.Add("mate.proszenyak2005@gmail.com");

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("E-mail riasztás sikeresen elküldve!");
        }

        private int GetPreviousLowestPrice()
        {
            try
            {
                if (System.IO.File.Exists(stateFilePath))
                {
                    string content = System.IO.File.ReadAllText(stateFilePath);
                    if (int.TryParse(content, out int price))
                    {
                        return price;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Nem sikerült beolvasni a korábbi árat: {ex.Message}");
            }

            return int.MaxValue;
        }

        private void SaveNewLowestPrice(int price)
        {
            try
            {
                System.IO.File.WriteAllText(stateFilePath, price.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Nem sikerült elmenteni az új árat: {ex.Message}");
            }
        }
    }
}