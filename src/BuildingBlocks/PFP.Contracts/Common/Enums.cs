namespace PFP.Contracts.Common;

public enum ExpenseCategory
{
    Other = 0,
    Housing = 1,
    Utilities = 2,
    Groceries = 3,
    Transportation = 4,
    Healthcare = 5,
    Insurance = 6,
    Dining = 7,
    Entertainment = 8,
    Shopping = 9,
    Education = 10,
    Travel = 11,
    Savings = 12
}

public enum PaymentMethod
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    DigitalWallet = 4
}

public enum BudgetPeriod
{
    Weekly = 0,
    Monthly = 1
}

public enum BudgetAlertLevel
{
    None = 0,
    Warning = 1,
    Exceeded = 2
}
