using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FlightDealTracker
{
    public class FlightDealTracker
    {
        private readonly ILogger _logger;
        private static readonly HttpClient client = new HttpClient();

        public FlightDealTracker(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FlightDealTracker>();
        }

        [Function("FlightDealTracker")]
        public async Task Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"A járatkeresõ elindult: {DateTime.Now}");

            string targetDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://sky-scrapper.p.rapidapi.com/api/v2/flights/searchFlightEverywhere?originEntityId=27544008&cabinClass=economy&journeyType=one_way&currency=HUF&date={targetDate}"),
                Headers =
                {
                    { "x-rapidapi-key", "744be71ac9msh451f0806b930e25p159563jsn5f857040ceae" },
                    { "x-rapidapi-host", "sky-scrapper.p.rapidapi.com" },
                },
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var jsonBody = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var flightResult = JsonSerializer.Deserialize<Rootobject>(jsonBody, options);

                    if (flightResult?.data?.results != null)
                    {
                        _logger.LogInformation("--- GLOBÁLIS OLCSÓ JÁRATOK KERESÉSE ---");

                        StringBuilder emailBody = new StringBuilder();
                        emailBody.AppendLine("25 000 Ft alatti nemzetközi repülõjegyek Budapestrõl:\n");

                        int matchCount = 0;

                        // Végigmegyünk az összes városon, amit a világon talált
                        foreach (var result in flightResult.data.results)
                        {
                            var destinationName = result.content?.location?.name;
                            var continentName = result.content?.location?.continent?.name;
                            var cheapestQuote = result.content?.flightQuotes?.cheapest;

                            if (cheapestQuote != null)
                            {
                                int rawPrice = cheapestQuote.rawPrice;
                                string formattedPrice = cheapestQuote.price;
                                bool isDirect = cheapestQuote.direct;
                                string flightType = isDirect ? "Közvetlen" : "Átszállásos";

                                if (rawPrice <= 25000)
                                {
                                    string skyCode = result.content?.location?.skyCode.ToLower();

                                    string linkMonth = DateTime.Now.AddMonths(1).ToString("yyMM");

                                    string bookingLink = $"https://www.skyscanner.hu/transport/flights/bud/{skyCode}/{linkMonth}/?adults=1&cabinclass=economy&rtn=0";

                                    string line = $"- {destinationName} ({continentName}): {formattedPrice} ({flightType})\n  Foglalás és részletek: {bookingLink}\n";
                                    _logger.LogInformation($"> TALÁLAT: {line}");
                                    emailBody.AppendLine(line);
                                    matchCount++;
                                }
                            }
                        }

                        if (matchCount > 0)
                        {
                            _logger.LogInformation($"{matchCount} olcsó desztinációt találtam. E-mail küldése...");

                            var smtpClient = new SmtpClient("smtp.gmail.com")
                            {
                                Port = 587,
                                Credentials = new NetworkCredential("only.ride.info@gmail.com", "fwbg hqpp ydbp plbv"),
                                EnableSsl = true,
                            };

                            var mailMessage = new MailMessage
                            {
                                From = new MailAddress("only.ride.info@gmail.com"),
                                Subject = $"Napi Olcsó Repülõjegyek ({matchCount} város)",
                                Body = emailBody.ToString(),
                            };

                            mailMessage.To.Add("mate.proszenyak2005@gmail.com");

                            await smtpClient.SendMailAsync(mailMessage);
                            _logger.LogInformation("Az e-mail sikeresen elküldve!");
                        }
                        else
                        {
                            _logger.LogInformation("Ma nem találtunk 25 000 Ft alatti járatot, nem küldünk e-mailt.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Nem érkezett értelmezhetõ adat az API-tól.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Hiba történt a futás során: {ex.Message}");
            }
        }
    }
}