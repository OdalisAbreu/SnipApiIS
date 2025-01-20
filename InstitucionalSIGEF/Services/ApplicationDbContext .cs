﻿using InstitucionalSIGEF.Models;
using Microsoft.EntityFrameworkCore;

namespace InstitucionalSIGEF.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        { }

        public DbSet<Log> Logs { get; set; } // Asegúrate de que Logs sea una propiedad DbSet<Log>
    }
}
