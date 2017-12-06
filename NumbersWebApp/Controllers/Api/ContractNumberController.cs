using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using AccountNumberTools.CreditCard.Methods;
using StackExchange.Redis;

namespace NumbersWebApp.Controllers.Api
{
    [RoutePrefix("api/{countryCode}/number/contract/{productType}")]
    public class ContractNumberController : ApiController
    {
        private const string _redisKey = "contractNumber";
        private readonly IDatabase _db;
        private readonly ConnectionMultiplexer _redis;

        public ContractNumberController()
        {
            _redis = ConnectionMultiplexer.Connect("redis-17550.c11.us-east-1-3.ec2.cloud.redislabs.com:17550");
            _db = _redis.GetDatabase();
        }

        [HttpGet, Route("")]
        // GET api/<controller>
        public async Task<IHttpActionResult> Get(string countryCode, string productType)
        {
            long number;
            var value = await _db.StringGetAsync(GetRedisKey(countryCode, productType));
            if (value.TryParse(out number))
                return Ok(BuildConctractNumber(number, countryCode.ToUpper().First()));
            return Ok((object)null);
        }

        [HttpPost, Route("")]
        // POST api/<controller>
        public async Task<IHttpActionResult> Post(string countryCode, string productType)
        {
            var newNumber = await _db.StringIncrementAsync(GetRedisKey(countryCode, productType));
            return Ok(BuildConctractNumber(newNumber, countryCode.ToUpper().First()));
        }

        private static string BuildConctractNumber(long newContractNumber, char countryLetter, DateTime? atDate = null)
        {
            var date = atDate ?? DateTime.Now;
            return $"{countryLetter}{date.ToString("yy", CultureInfo.InvariantCulture)}{IntToAnyBaseString(newContractNumber, "0123456789ABCDEFGHJKLMNOPQRSTUVWXYZ").PadLeft(5, '0')}";
        }

        private static string GetRedisKey(string countryCode, string productType, DateTime? atDate = null)
        {
            var date = atDate ?? DateTime.Now;
            return $"{countryCode.ToLower()}:{productType.ToLower()}:{_redisKey}:{date.ToString("yy", CultureInfo.InvariantCulture)}";
        }

        public static string IntToAnyBaseString(long value, string baseChars)
        {
            // 32 is the worst cast buffer size for base 2 and int.MaxValue
            var i = 32;
            var buffer = new char[i];
            var targetBase = baseChars.Length;

            do
            {
                buffer[--i] = baseChars[(int)(value % targetBase)];
                value = value / targetBase;
            }
            while (value > 0);

            var result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }
        protected override void Dispose(bool disposing)
        {
            _redis.Dispose();
            base.Dispose(disposing);
        }
    }
}
