using ECommerce.Domain.Interfaces;
using equals.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;

namespace ECommerce.Repository.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            //var connectionString = "Host=91.132.49.135;Port=5432;Database=ecommerce;Username=postgres;Password=lgaSxoGWyJdm8av5vBoYs0wR4TyIVl2fYnAiagId5w774AwfCs7ssIiI6AQgv1Uq;SSL Mode=Allow;Trust Server Certificate=true";
            //var connectionString = "Host=dpg-d11f8063jp1c73eu87t0-a.oregon-postgres.render.com;Databa se=commerce_last;Username=commerce_last_user;Password=Qe3hgFxOVTPn8JlomlpY3q6XvpKtvXmy;Port=5432;SSL Mode=Require;Trust Server Certificate=true";
            var connectionString = "Host=localhost;Port=5432;Database=ecommercenewone;Username=postgres;Password=postgres";



            optionsBuilder.UseNpgsql(connectionString);

            var currentUserService = new Mock<ICurrentUserService>().Object;
            var dateTimeService = new Mock<IDateTime>().Object;

            return new ApplicationDbContext(optionsBuilder.Options, currentUserService, dateTimeService);
        }
    }
}
