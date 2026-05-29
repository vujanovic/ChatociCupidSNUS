namespace ChatociCupidSNUS.Models
{
    public class Person
    {
        public string Username { get; set; }
        public int Age { get; set; }
        public string City { get; set; }
        public string PhoneNumber { get; set; }

        public Person(string username, int age, string city, string phoneNumber)
        {
            this.Username = username;
            this.Age = age;
            this.City = city;
            this.PhoneNumber = phoneNumber;
        }
    }
}
