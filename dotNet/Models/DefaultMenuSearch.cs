namespace Atomus.Control.Menu.Models
{
    public class DefaultMenuSearch : Mvc.Models
    {
        public string DatabaseName { get; set; }
        public string ProcedureID { get; set; }
        public string MENU_NAME { get; set; }
        public decimal MENU_ID { get; set; }
        public decimal ASSEMBLY_ID { get; set; }
    }
}