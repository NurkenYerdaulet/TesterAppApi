using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TesterAppApi.Models.Entitles;

namespace TesterAppApi.Models
{
    public class TesterDbContext: DbContext
    {
        public TesterDbContext(DbContextOptions<TesterDbContext> options) : base(options)
        { }
        public DbSet<User> users { get; set; }
        //public DbSet<Question> questions { get; set; }
        //public DbSet<Common> commons { get; set; }
        public DbSet<Role> Roles { get; set; }
    }
}
