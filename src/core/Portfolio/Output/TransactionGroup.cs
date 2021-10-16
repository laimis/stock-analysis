namespace core.Portfolio.Output
{
    public class TransactionGroup
    {
        public TransactionGroup(string name, TransactionList transactions)
        {
            Name = name;
            Transactions = transactions;
        }

        public string Name { get; set; }
        public TransactionList Transactions { get; set; }
    }
}