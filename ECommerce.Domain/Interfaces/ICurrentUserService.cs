using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace equals.Domain.Interfaces
{
    /// <summary>
    /// Interface for providing the current user context
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Gets the current authenticated user's ID
        /// </summary>
        string UserId { get; }
    }
}
