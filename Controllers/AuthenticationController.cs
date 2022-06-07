using AuthApi.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthenticationController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public string Encrypt(string decrypted)
        {


            MD5 md5 = new MD5CryptoServiceProvider();

            //compute hash from the bytes of text  
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(decrypted));

            //get hash result after compute it  
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //change it into 2 hexadecimal digits  
                //for each byte  
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();





        }


        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser(RegisterViewModel data)
        {


            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid");
                }

                if (data.UserID != null)
                {
                    using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        con.Open();

                        SqlCommand cmd = new SqlCommand("Update tblUser set FullName = @FullName, DOB = @DOB, Mobile= @Mobile, Gender= @Gender where UserID = @UserID", con);

                        cmd.Parameters.AddWithValue("@UserID", data.UserID);
                        cmd.Parameters.AddWithValue("@FullName", data.FullName);
                        cmd.Parameters.AddWithValue("@DOB", data.DOB);

                        cmd.Parameters.AddWithValue("@Mobile", data.Mobile);
                        cmd.Parameters.AddWithValue("@Gender", data.Gender);

                        cmd.ExecuteNonQuery();

                        con.Close();


                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        con.Open();

                        string query = "select Count(*) from tblUser where Email = '" + data.Email + "'";
                        SqlCommand cmdTwo = new SqlCommand(query, con);
                        var result = cmdTwo.ExecuteScalar();
                        int i = Convert.ToInt32(result);
                        if (i == 0)
                        {
                            string password = Encrypt(data.Password);



                            SqlCommand cmd = new SqlCommand("Insert Into tblUser (UserID, FullName, Email, Password, SecurityQuestionID, SecurityAns) Values (@UserID, @FullName, @Email, @Password, @SecurityQuestionID, @SecurityAns)", con);
                            string userId = Guid.NewGuid().ToString();

                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@Email", data.Email);
                            cmd.Parameters.AddWithValue("@FullName", data.FullName);
                            cmd.Parameters.AddWithValue("@Password", password);
                            cmd.Parameters.AddWithValue("@SecurityQuestionID", data.SecurityQuestionID);
                            cmd.Parameters.AddWithValue("@SecurityAns", data.SecurityAns);

                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            return BadRequest("Email already exist!!");
                        }




                        con.Close();


                    }
                }






                return Ok();
            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {
                    return BadRequest(ex.InnerException.Message);
                }
                else
                {
                    return BadRequest(ex.Message);
                }
            }


        }


        [HttpGet("getSecurityQuestions")]
        public async Task<IActionResult> GetSecurityQuestions()
        {
            var security = new List<SecurityQuestionViewModel>();
            var query = $"select SecurityQuestionID, SecurityQuestion from SecurityQuestions";

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var sec = new SecurityQuestionViewModel
                        {
                            SecurityQuestionID = reader["SecurityQuestionID"].ToString(),
                            SecurityQuestion = reader["SecurityQuestion"].ToString()
                        };
                        security.Add(sec);
                    }
                }
                conn.Close();
            }


            var data = new { SECURITY = security };
            return Ok(data);

        }


        [HttpGet("getProfileData")]
        public async Task<IActionResult> GetProfileData(string id)
        {
            var profile = new List<RegisterViewModel>();
            var query = $"Select Gender, ISNULL(DOB, '') as DOB, Mobile, FullName,  Email , tblUser.SecurityQuestionID, SecurityQuestions.SecurityQuestion, SecurityAns From tblUser join SecurityQuestions on tblUser.SecurityQuestionID = SecurityQuestions.SecurityQuestionID where UserID = '{id}'";

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = query;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var pro = new RegisterViewModel
                        {


                            Email = reader["Email"].ToString(),
                            Gender = reader["Gender"].ToString(),

                            DOB = Convert.ToDateTime(reader["DOB"]),

                            Mobile = reader["Mobile"].ToString(),
                            FullName = reader["FullName"].ToString(),
                            SecurityQuestionID = reader["SecurityQuestionID"].ToString(),
                            SecurityQuestion = reader["SecurityQuestion"].ToString(),
                            SecurityAns = reader["SecurityAns"].ToString(),
                        };
                        profile.Add(pro);
                    }
                }
                conn.Close();
            }




            var data = new { PROFILE = profile };
            return Ok(data);

        }


        [HttpGet("verifyUserWithSecurity")]
        public async Task<IActionResult> VerifyUserWithSecurity(string email, string question, string ans)
        {

            int i;

            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                con.Open();
                // string query = "select Count(*) from tblUser where Email = '" + data.Email + "'";
                string query = "select Count(*) from tblUser where Email ='" + email + "' and SecurityQuestionID= '" + question + "' and SecurityAns='" + ans + "'";
                SqlCommand cmd = new SqlCommand(query, con);
                var result = cmd.ExecuteScalar();
                i = Convert.ToInt32(result);
                con.Close();
            }


            return Ok(i);

        }






        [HttpPost("login")]
        public async Task<IActionResult> Login(RegisterViewModel model)
        {



            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid");
                }

                LoginViewModel user = new LoginViewModel();


                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    string password = Encrypt(model.Password);

                    SqlCommand cmd = new SqlCommand("SELECT UserID, Email FROM tblUser where Email=@Email and Password=@Password", con);

                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.CommandType = CommandType.Text;


                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {

                        if (rdr.HasRows)
                        {
                            rdr.Read(); // get the first row

                            user.UserID = rdr.GetString(0);
                            user.Email = rdr.GetString(1);
                        }
                        else
                        {
                            return BadRequest("Wrong Email or Password");
                        }
                    }



                    con.Close();


                }

                var data = new { USER = user };
                return Ok(data);


            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {
                    return BadRequest(ex.InnerException.Message);
                }
                else
                {
                    return BadRequest(ex.Message);
                }
            }


        }


        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword(RegisterViewModel data)
        {


            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid");
                }


                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();

                    string password = Encrypt(data.Password);

                    SqlCommand cmd = new SqlCommand("Update tblUser set Password = @Password where Email = @Email", con);

                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@Email", data.Email);


                    cmd.ExecuteNonQuery();

                    con.Close();


                }


                return Ok();
            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {
                    return BadRequest(ex.InnerException.Message);
                }
                else
                {
                    return BadRequest(ex.Message);
                }
            }


        }

    }
}
