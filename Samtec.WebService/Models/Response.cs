using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Samtec.WebService.Models
{

    public class Response
    {
        public string JobType { get; set; }
        public string ResultCode { get; set; }
        public string InvoiceUID { get; set; }

        public int GetInvoiceDevDailyNum()
        {
            var props = this.ResultCode.Split('|');
            if(props.Length >= 3)
            {
                int outVal = 0;
                if(int.TryParse(props[2], out outVal))
                    return outVal;
                return -1;
            }
            return -1;
        }
    }

}
