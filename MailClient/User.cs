using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailClient
{
    [Serializable]
    public class User : ICloneable
    {
        public string Name { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int SelectedEmailBoxIndex { get; set; }
        public List<EmailBox> EmailBoxes { get; set; }

        public User() { }

        public override string ToString()
        {
            string str = String.Format("[Name: {0}; Login: {1}; Password: {2}; EmailBoxes: [", this.Name, this.Login, this.Password);

            if (this.EmailBoxes != null)
            {
                for (int i = 0; i < this.EmailBoxes.Count; i++)
                {
                    str += " EmailBox" + (i + 1) + ": " + this.EmailBoxes[i].ToString() + ";";
                }
            }

            str += "]]";
            return str;
        }

        public override bool Equals(object obj)
        {
            return this.ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public object Clone()
        {
            List<EmailBox> newCopy = new List<EmailBox>();
            for (int i = 0; i < this.EmailBoxes.Count; i++)
            {
                newCopy.Add(this.EmailBoxes[i]);
            }

            return new User
            {
                Name = this.Name,
                Login = this.Login,
                Password = this.Password,
                EmailBoxes = newCopy
            };
        }
    }
}
