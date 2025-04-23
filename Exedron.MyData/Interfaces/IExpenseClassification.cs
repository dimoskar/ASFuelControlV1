using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exedron.MyData.Interfaces
{
    public interface IExpenseClassification : IRequestMember
    {
        string ClassificationType { set; get; }
        string ClassificationCategory { set; get; }
        decimal Amount { set; get; }
        Byte Id { set; get; }
    }
}
