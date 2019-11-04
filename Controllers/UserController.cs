using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ingos_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace Ingos_API.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public UserController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private string GetConnString()
        {
            return configuration.GetConnectionString("Ingos_db");
        }

        // GET api/user
        [Route("api/[controller]")]
        [HttpGet]
        public ActionResult Get()
        {
            string cs = GetConnString();

            using (var con = new NpgsqlConnection(cs))
            {
                con.Open();

                string queryString = @"select json_agg(result)
                                        from (select u.id,
                                                     u.first_name,
                                                     u.mid_name,
                                                     u.last_name,
                                                     u.last_name || ' ' || u.first_name || ' ' || u.mid_name                             full_name,
                                                     town,
                                                     (select json_agg(r1)
                                                      from (select t.*,
                                                                   (select json_agg(r2)
                                                                    from (select id, rating_count as count, rating_value as value
                                                                          from ratings rt
                                                                          where rt.technology_id = t.id
                                                                            and rt.user_id = u.id) r2) as rating
                                                            from technology t
                                                            where id in (select technology_id from ratings where user_id = u.id)) r1) as tech
                                              from users u) result;";

                var cmd = new NpgsqlCommand(queryString, con);

                try
                {
                    return Ok(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message.Replace(Environment.NewLine, ""));
                }
            }
        }

        // GET api/user/5
        [Route("api/[controller]")]
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/user
        [Route("api/[controller]")]
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/user/5
        [Route("api/[controller]")]
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/user/5
        [Route("api/[controller]")]
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [Route("api/[controller]/[action]")]
        [HttpPost]
        public async Task<HttpResponseMessage> SetRating([FromBody] JObject data)
        {
            if (data == null)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "POST body is null" };

            string cs = GetConnString();
            var userId = (data["userId"]).ToString();
            var technologyId = (data["technologyId"]).ToString();
            var newRatingValue = (data["newRatingValue"]).ToString();

            using (var con = new NpgsqlConnection(cs))
            {
                await con.OpenAsync();
                string queryString = $@"select setrating(@userId, @technologyId, @newRatingValue);";

                var cmd = new NpgsqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("userId", int.Parse(userId));
                cmd.Parameters.AddWithValue("technologyId", int.Parse(technologyId));
                cmd.Parameters.AddWithValue("newRatingValue", int.Parse(newRatingValue));

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = e.Message.Replace(Environment.NewLine, "") };
                }
            }

            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = "Rating changed successfully" };

        }

        [Route("api/[controller]/[action]")]
        [HttpPost]
        public async Task<HttpResponseMessage> AddRating([FromBody] JObject data)
        {
            if (data == null)
                return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "POST body is null" };

            string cs = GetConnString();
            var userId = (data["userId"]).ToString();
            var technologyId = (data["technologyId"]).ToString();
            var newRatingValue = (data["newRatingValue"]).ToString();

            using (var con = new NpgsqlConnection(cs))
            {
                await con.OpenAsync();
                string queryString = $@"select addrating(@userId, @technologyId, @newRatingValue);";

                var cmd = new NpgsqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("userId", int.Parse(userId));
                cmd.Parameters.AddWithValue("technologyId", int.Parse(technologyId));
                cmd.Parameters.AddWithValue("newRatingValue", int.Parse(newRatingValue));

                try
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = e.Message.Replace(Environment.NewLine, "") };
                }
            }

            return new HttpResponseMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = "Rating changed successfully" };
        }

        [Route("api/[controller]/[action]")]
        [HttpPost]
        public IActionResult RegisterUser([FromBody] JObject data)
        {
            if (data == null)
                return BadRequest("POST body is null");

            string cs = GetConnString();
            var first_name = (data["first_name"]).ToString();
            var mid_name = (data["mid_name"]).ToString();
            var last_name = (data["last_name"]).ToString();
            var town = (data["town"]).ToString();
            var email = (data["email"]).ToString();
            var password = (data["password"]).ToString();

            using (var con = new NpgsqlConnection(cs))
            {
                con.Open();
                string queryString = $@"select registeruser(@first_name, @mid_name, @last_name, @town, @email, @password);";

                var cmd = new NpgsqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("first_name", first_name);
                cmd.Parameters.AddWithValue("mid_name", mid_name);
                cmd.Parameters.AddWithValue("last_name", last_name);
                cmd.Parameters.AddWithValue("town", town);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);

                try
                {
                    return Ok(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message.Replace(Environment.NewLine, ""));
                }
            }
        }

        [Route("api/[controller]/[action]")]
        [HttpPost]
        public IActionResult Login([FromBody] JObject data)
        {
            if (data == null)
                return BadRequest("POST body is null");

            string cs = GetConnString();
            var email = (data["email"]).ToString();
            var password = (data["password"]).ToString();

            using (var con = new NpgsqlConnection(cs))
            {
                con.Open();
                string queryString = $@"select * from login(@email, @password);";

                var cmd = new NpgsqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);

                try
                {
                    return Ok(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message.Replace(Environment.NewLine, ""));
                }
            }
        }

        [Route("api/[controller]/[action]")]
        [HttpPost]
        public IActionResult UpdateUser([FromBody] JObject data)
        {
            if (data == null)
                return BadRequest("POST body is null");

            string cs = GetConnString();
            var id = (data["id"]).ToString();
            var first_name = (data["first_name"]).ToString();
            var mid_name = (data["mid_name"]).ToString();
            var last_name = (data["last_name"]).ToString();
            var town = (data["town"]).ToString();
            var email = (data["email"]).ToString();
            var password = (data["password"]).ToString();

            using (var con = new NpgsqlConnection(cs))
            {
                con.Open();
                string queryString = $@"select updateuser(@id, @first_name, @mid_name, @last_name, @town, @email, @password);";

                var cmd = new NpgsqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("id", int.Parse(id));
                cmd.Parameters.AddWithValue("first_name", first_name);
                cmd.Parameters.AddWithValue("mid_name", mid_name);
                cmd.Parameters.AddWithValue("last_name", last_name);
                cmd.Parameters.AddWithValue("town", town);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);

                try
                {
                    return Ok(cmd.ExecuteScalar());
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message.Replace(Environment.NewLine, ""));
                }
            }
        }
    }
}
