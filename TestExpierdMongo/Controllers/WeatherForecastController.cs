using Microsoft.AspNetCore.Mvc;

namespace TestExpierdMongo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        private readonly IUserRepository userRepository;
        public WeatherForecastController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };



        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get(CancellationToken cancellationToken)
        {

            for (int i = 0; i < 1000; i++)
            {
                var user = new User
                {
                    Id=Guid.NewGuid().ToString("N"),
                    CreationTime = DateTime.Now,
                    Name = $"User {i}"
                };
                await userRepository.AddUser(user, cancellationToken);
                await Task.Delay(500 * i);
            }

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
