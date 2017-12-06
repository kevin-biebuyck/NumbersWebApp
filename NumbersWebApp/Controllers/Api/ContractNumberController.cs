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
    [RoutePrefix("api/number/contract")]
    public class ContractNumberController : ApiController
    {
        private const string _redisKey = "contractNumber";
        private const string _countryLetter = "W";
        private readonly IDatabase _db;

        public ContractNumberController()
        {
            var redis = ConnectionMultiplexer.Connect("localhost");
            _db = redis.GetDatabase();
        }

        [HttpGet, Route("")]
        // GET api/<controller>
        public async Task<IHttpActionResult> Get()
        {
            long number;
            var value = await _db.StringGetAsync(GetRedisKey());
            if (value.TryParse(out number))
                return Ok(BuildConctractNumber(number));
            return Ok((object)null);
        }

        [HttpPost, Route("")]
        // POST api/<controller>
        public async Task<IHttpActionResult> Post([FromBody]string value)
        {
            var newNumber = await _db.StringIncrementAsync(GetRedisKey());
            return Ok(BuildConctractNumber(newNumber));
        }

        private static string BuildConctractNumber(long newContractNumber, DateTime? atDate = null)
        {
            var date = atDate ?? DateTime.Now;
            return $"{_countryLetter}{date.ToString("yy", CultureInfo.InvariantCulture)}{IntToAnyBaseString(newContractNumber, "0123456789ABCDEFGHJKLMNOPQRSTUVWXYZ").PadLeft(5, '0')}";
        }

        private static string GetRedisKey(DateTime? atDate = null)
        {
            var date = atDate ?? DateTime.Now;
            return $"{_redisKey}:{date.ToString("yy", CultureInfo.InvariantCulture)}";
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
    }
}
