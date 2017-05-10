﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DETI_MakerLab
{
    [Serializable()]
    class Staff
    {
        private int _employeeNum;
        private String _firstName;
        private String _lastName;
        private String _email;
        private String _passwordHash;
        private String _pathToImage;

        public int EmployeeNum
        {
            get { return _employeeNum; }
            set
            {
                if (value > 100000)
                    throw new Exception("Invalid EmployeeNum");
                _employeeNum = value;
            }
        }

        public String FirstName
        {
            get { return _firstName; }
            set { _firstName = value; }
        }

        public String LastName
        {
            get { return _lastName; }
            set { _lastName = value; }
        }

        public String Email
        {
            get { return _email; }
            set
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(value);
                    if (addr.Address != value)
                        throw new Exception("Invalid email");
                    _email = value;
                }
                catch
                {
                    throw new Exception("Invalid email");
                }
            }
        }

        public String PasswordHash
        {
            get { return _passwordHash; }
            set
            {
                if (value == null | String.IsNullOrEmpty(value))
                    throw new Exception("Invalid password");
                byte[] salt;
                new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
                var pbkdf2 = new Rfc2898DeriveBytes(value, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);
                byte[] hashBytes = new byte[36];
                Array.Copy(salt, 0, hashBytes, 0, 16);
                Array.Copy(hash, 0, hashBytes, 16, 20);
                _passwordHash = Convert.ToBase64String(hashBytes);
            }
        }

        public String PathToImage
        {
            get { return _pathToImage; }
            set { _pathToImage = value; }
        }

        public override String ToString()
        {
            return "Staff:" + _employeeNum.ToString();
        }

        public Staff() { }

        public Staff(int EmployeeNum, String FirstName, String LastName,
            String Email, String PasswordHash, String PathToImage)
        {
            this.EmployeeNum = EmployeeNum;
            this.FirstName = FirstName;
            this.Email = Email;
            this.PasswordHash = PasswordHash;
            this.PathToImage = PathToImage;
        }
    }
}
