using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthApi.ViewModels
{
    public class RegisterViewModel
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Gender { get; set; }
        public DateTime? DOB { get; set; }
        public string Mobile { get; set; }
        public string FullName { get; set; }
        public string SecurityQuestionID { get; set; }
        public string SecurityAns { get; set; }

        public string SecurityQuestion { get; set; }


    }
}
