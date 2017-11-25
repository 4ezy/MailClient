using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailClient
{
    [Serializable]
    public class User
    {
        public string Name { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public List<EmailBox> EmailBoxes { get; set; }

        public User() { }

        public override string ToString()
        {
            return string.Format("[Name: {0}; Login: {1}; Password: {2}]", this.Name, this.Login, this.Password);
        }

        public override bool Equals(object obj)
        {
            return this.ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
