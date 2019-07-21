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
        public IActionResult Get()
        {
            string cs = GetConnString();

            using (var con = new SqlConnection(cs))
            {
                con.Open();

                string queryString = @"declare @result nvarchar(max);
                                        set @result = (select u.id,
                                               u.first_name,
                                               u.mid_name,
                                               u.last_name,
                                               u.last_name + ' ' + u.first_name + ' ' + u.mid_name full_name,
                                               town,
                                               tech.id                                             id,
                                               tech.name                                           name,
                                               tech.description                                    description,
                                               rating.id                                           id,
                                               rating.rating_count                                 count,
                                               rating.rating_value                                 value
                                        from users u
                                                 left join ratings rating on u.id = rating.user_id
                                                 left join technology tech on rating.technology_id = tech.id FOR JSON AUTO)
                                        select @result;";

                SqlCommand cmd = new SqlCommand(queryString, con);

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

            using (var con = new SqlConnection(cs))
            {
                await con.OpenAsync();
                string queryString = $@"declare @ratingId bigint;
                                        declare @ratingValue int;
                                        declare @ratingCount int;

                                        if EXISTS(select *
                                                  from ratings
                                                  where user_id = @userId
                                                    and technology_id = @technologyId)
                                            begin
                                                set @ratingId = (select id from ratings where user_id = @userId and technology_id = @technologyId);
                                                set @ratingValue = (select rating_value from ratings where user_id = @userId and technology_id = @technologyId);
                                                set @ratingCount = (select rating_count from ratings where user_id = @userId and technology_id = @technologyId);

                                                update ratings
                                                set rating_count = @ratingCount + 1, rating_value = @ratingValue + @newRatingValue
                                                where id = @ratingId;
                                            end
                                        else
                                            begin
                                                insert into ratings (user_id, technology_id, rating_count, rating_value)
                                                values (@userId, @technologyId, 1, @newRatingValue)
                                            end;";
                SqlCommand cmd = new SqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("userId", userId);
                cmd.Parameters.AddWithValue("technologyId", technologyId);
                cmd.Parameters.AddWithValue("newRatingValue", newRatingValue);

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

            using (var con = new SqlConnection(cs))
            {
                await con.OpenAsync();
                string queryString = $@"if not exists(select * from ratings where user_id = @userId and technology_id = @technologyId)
                                        begin
                                            insert into ratings (user_id, technology_id, rating_count, rating_value) values (@userId, @technologyId, 1, @newRatingValue);
                                        end
                                    else raiserror ('Ошибка! Сотрудник уже имеет выбранную технологию.', 16, 16);";

                SqlCommand cmd = new SqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("userId", userId);
                cmd.Parameters.AddWithValue("technologyId", technologyId);
                cmd.Parameters.AddWithValue("newRatingValue", newRatingValue);

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

            using (var con = new SqlConnection(cs))
            {
                con.Open();
                string queryString = $@"declare @message varchar(100);
                                        if not exists(select * from users where email = @email)
                                            begin
                                                insert into users (first_name, mid_name, last_name, town, email, password)
                                                values (@first_name, @mid_name, @last_name, @town, @email, @password);
                                                select * from users where email = @email for json auto;
                                            end
                                        else
                                            begin
                                                set @message = 'Email ' + @email + ' already exists';
                                                raiserror (@message, 16, 16);
                                            end;";

                SqlCommand cmd = new SqlCommand(queryString, con);
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

            using (var con = new SqlConnection(cs))
            {
                con.Open();
                string queryString = $@"declare @message varchar(100);
                                        if exists(select * from users where email = @email and password = @password)
                                            begin
                                                select u.id,
                                                       u.first_name,
                                                       u.mid_name,
                                                       u.last_name,
                                                       u.last_name + ' ' + u.first_name + ' ' + u.mid_name full_name,
                                                       u.town,
                                                       u.email
                                                from users u where email = @email for json auto;
                                            end
                                        else
                                            begin
                                                set @message = 'Error! Wrong email or password';
                                                raiserror (@message, 16, 16);
                                            end;";

                SqlCommand cmd = new SqlCommand(queryString, con);
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

            using (var con = new SqlConnection(cs))
            {
                con.Open();
                string queryString = $@"declare @message varchar(100);
                                        if exists(select * from users where id = @id)
                                            begin

                                                update users set
                                                                 first_name = @first_name,
                                                                 mid_name = @mid_name,
                                                                 last_name = @last_name,
                                                                 town = @town,
                                                                 email = @email,
                                                                 password = @password
                                                where id = @id;

                                                select u.id,
                                                       u.first_name,
                                                       u.mid_name,
                                                       u.last_name,
                                                       u.last_name + ' ' + u.first_name + ' ' + u.mid_name full_name,
                                                       u.town,
                                                       u.email
                                                from users u where u.id = @id for json auto;

                                            end
                                        else
                                            begin
                                                set @message = 'Error! Not Found user with email ' + @email;
                                                raiserror (@message, 16, 16);
                                            end;";

                SqlCommand cmd = new SqlCommand(queryString, con);
                cmd.Parameters.AddWithValue("id", id);
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
