namespace core.Admin
{
    public class EmailTransactionGroup
    {
        public EmailTransactionGroup(string name, EmailTransactionList transactions)
        {
            this.Name = name;
            this.Transactions = transactions;
        }

        public string Name { get; set; }
        public EmailTransactionList Transactions { get; set; }
    }
}