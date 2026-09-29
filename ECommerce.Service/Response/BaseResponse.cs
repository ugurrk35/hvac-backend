using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Response
{
    public class BaseResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();
        public static BaseResponse CreateSuccess(string message = "İşlem başarıyla tamamlandı")
        {
            return new BaseResponse
            {
                Success = true,
                Message = message
            };
        }

        public static BaseResponse CreateFailure(string message, List<string> errors = null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
