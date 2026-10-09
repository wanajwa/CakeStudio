namespace CakeStudio.Models
{
    public class Customer : User
    {
        public override string GetRoleName()
        {
            return "Customer";
        }
    }


}
