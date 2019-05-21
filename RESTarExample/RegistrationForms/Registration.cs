using System;
using RESTar.Resources;
using RESTar.Resources.Templates;
using static RESTar.Method;

// ReSharper disable All
#pragma warning disable 1591

namespace RESTarExample.RegistrationForms
{
    [RESTar(GET, PATCH)]
    public class Registration : Form<Registration>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        // The client can load the view from here
        public string Html => "/registration.html";

        private void Validate()
        {
            if (FirstName.Length > 1000)
                throw new Exception("Don't include huge first names!");
            if (LastName.Length > 1000)
                throw new Exception("Don't include huge last names!");
            if (!Email.Contains("@"))
                throw new Exception("That's no email address!");
            if (string.IsNullOrWhiteSpace(Password))
                throw new Exception("Invalid password!");
        }

        protected override void PreSubmit() => Validate();

        protected override void PostSubmit()
        {
            // do something useful with the filled and validated form!
        }
    }
}