using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AccountNumberTools.CreditCard.Methods;
using StackExchange.Redis;

namespace NumbersWebApp.Controllers.Api
{
    [RoutePrefix("api/number/card")]
    public class CardNumberController : ApiController
    {
        private const string _redisKey = "cardNumber";
        private readonly IDatabase _db;

        public CardNumberController()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            _db = redis.GetDatabase();
        }

        [HttpGet, Route("")]
        // GET api/<controller>
        public async Task<IHttpActionResult> Get()
        {
            long number;
            var value = await _db.StringGetAsync(_redisKey);
            if (value.TryParse(out number))
                return Ok(BuildCardNumber(number));
            return Ok((object)null);
        }

        [HttpPost, Route("")]
        // POST api/<controller>
        public async Task<IHttpActionResult> Post([FromBody]string value)
        {
            var newValue = await _db.StringIncrementAsync(_redisKey);
            return Ok(BuildCardNumber(newValue));
        }

        private static string BuildCardNumber(long newCardNumber)
        {
            var luhnCheckDigit = new ValidationMethodLuhn(0, int.MaxValue).CalculateCheckDigit(newCardNumber.ToString());
            var result = newCardNumber + luhnCheckDigit;
            return result;
        }
    }
}