namespace core.Admin
{
    public class EmailTransactionGroup
    {
        public EmailTransactionGroup(string name, EmailTransactionList transactions)
        {
            Name = name;
            Transactions = transactions;
        }

        public string Name { get; set; }
        public EmailTransactionList Transactions { get; set; }
    }
}