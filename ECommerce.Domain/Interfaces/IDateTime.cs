using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Interfaces
{
    /// <summary>
    /// Interface for providing date and time services
    /// </summary>
    public interface IDateTime
    {
        /// <summary>
        /// Gets the current date and time
        /// </summary>
        DateTime Now { get; }
    }
}
