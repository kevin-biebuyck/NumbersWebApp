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
    [RoutePrefix("api/{countryCode}/number/card/{productType}")]
    public class CardNumberController : ApiController
    {
        private const string _redisKey = "cardNumber";
        private readonly IDatabase _db;
        private readonly ConnectionMultiplexer _redis;

        public CardNumberController()
        {
            _redis = ConnectionMultiplexer.Connect("redis-17550.c11.us-east-1-3.ec2.cloud.redislabs.com:17550");
            _db = _redis.GetDatabase();
        }

        [HttpGet, Route("")]
        // GET api/<controller>
        public async Task<IHttpActionResult> Get(string countryCode, string productType)
        {
            long number;
            var value = await _db.StringGetAsync(BuildRedisKey(countryCode, productType));
            if (value.TryParse(out number))
                return Ok(BuildCardNumber(number));
            return Ok((object)null);
        }

        [HttpPost, Route("")]
        // POST api/<controller>
        public async Task<IHttpActionResult> Post(string countryCode, string productType)
        {
            var newValue = await _db.StringIncrementAsync(BuildRedisKey(countryCode, productType));
            return Ok(BuildCardNumber(newValue));
        }

        private static RedisKey BuildRedisKey(string countryCode, string productType)
        {
            return $"{countryCode.ToLower()}:{productType.ToLower()}:{_redisKey}";
        }


        private static string BuildCardNumber(long newCardNumber)
        {
            var newCardNumberString = "601243" + newCardNumber.ToString().PadLeft(17, '0');
            var luhnCheckDigit = new ValidationMethodLuhn(0, int.MaxValue).CalculateCheckDigit(newCardNumberString);
            var result = newCardNumberString + luhnCheckDigit;
            return result;
        }

        protected override void Dispose(bool disposing)
        {
            _redis.Dispose();
            base.Dispose(disposing);
        }
    }
}