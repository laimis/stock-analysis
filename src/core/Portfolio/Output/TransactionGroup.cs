namespace core.Portfolio.Output
{
    public class TransactionGroup
    {
        public TransactionGroup(string name, TransactionList transactions)
        {
            this.Name = name;
            this.Transactions = transactions;
        }

        public string Name { get; set; }
        public TransactionList Transactions { get; set; }
    }
}