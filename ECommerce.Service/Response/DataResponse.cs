using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Service.Response
{
    public class DataResponse<T> : BaseResponse
    {
        public T Data { get; set; }

        public static DataResponse<T> CreateSuccess(T data, string message = null)
        {
            return new DataResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static DataResponse<T> CreateFailure(string message, List<string> errors = null)
        {
            return new DataResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}
