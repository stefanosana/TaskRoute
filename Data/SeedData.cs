using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskRoute.Data;
using TaskRoute.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TaskRoute.Data
{
    public static class SeedData
    {
        public static async System.Threading.Tasks.Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Crea ruoli se non esistono
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole { Name = "User" });
            }

            if (!await roleManager.RoleExistsAsync("Editor"))
            {
                await roleManager.CreateAsync(new IdentityRole { Name = "Editor" });
            }

            // Se non ci sono utenti, crea l'amministratore
            if (!context.Users.Any())
            {
                string UserName = "alfa@alfal.it";
                string TestUserPw = "P@ssw0rd";

                var user = await userManager.FindByNameAsync(UserName);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = UserName,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, TestUserPw);

                    if (!result.Succeeded)
                    {
                        throw new Exception("Errore durante la creazione dell'utente amministratore.");
                    }
                }

                // Aggiungi l'utente al ruolo "Administrator"
                if (!await roleManager.RoleExistsAsync("Administrator"))
                {
                    throw new Exception("Ruolo <Administrator> inesistente");
                }
                await userManager.AddToRoleAsync(user, "Administrator");
            }

            // Ottieni gli utenti dal database
            var users = await context.Users.ToListAsync();

            // Verifica che ci sia almeno un utente nel database
            if (!users.Any())
            {
                throw new Exception("Nessun utente trovato nel database. Assicurati che gli utenti siano stati creati correttamente.");
            }

            // Se non ci sono task, popola il database con dati di esempio
            if (!context.Commissions.Any())
            {
                // Inserisci delle posizioni di esempio
                var locations = new Location[]
                {
                    new Location
                    {
                        Name = "Supermercato Alfa",
                        Address = "Via Roma, 123",
                        City = "Roma",
                        PostalCode = "00100",
                        Country = "Italia",
                        Latitude = 41.890238,
                        Longitude = 12.492244
                    },
                    new Location
                    {
                        Name = "Ufficio Postale",
                        Address = "Corso Italia, 456",
                        City = "Milano",
                        PostalCode = "20100",
                        Country = "Italia",
                        Latitude = 45.464211,
                        Longitude = 9.190027
                    },
                    new Location
                    {
                        Name = "Lavanderia XYZ",
                        Address = "Viale della Libertà, 789",
                        City = "Torino",
                        PostalCode = "10100",
                        Country = "Italia",
                        Latitude = 45.070302,
                        Longitude = 7.686899
                    }
                };

                foreach (var location in locations)
                {
                    context.Locations.Add(location);
                }
                await context.SaveChangesAsync();

                // Ottieni le posizioni dal database
                var dbLocations = await context.Locations.ToListAsync();

                // Inserisci delle commissioni di esempio e associale agli utenti
                var tasks = new TaskRoute.Models.Commission[]
                {
                    new TaskRoute.Models.Commission
                    {
                        Title = "Comprare latte",
                        Description = "Acquistare 2 litri di latte intero.",
                        DueDate = DateTime.Now.AddDays(1),
                        IsCompleted = false,
                        UserId = users[0].Id, // Associa al primo utente
                        LocationId = dbLocations[0].Id // Associa alla prima posizione
                    },
                    new TaskRoute.Models.Commission
                    {
                        Title = "Pagare bolletta elettrica",
                        Description = "Versare la bolletta online.",
                        DueDate = DateTime.Now.AddDays(3),
                        IsCompleted = false,
                        UserId = users[0].Id, // Associa al primo utente
                        LocationId = dbLocations[1].Id // Associa alla seconda posizione
                    }
                };
                

                // Se c'è più di un utente, aggiungi un task per il secondo utente
                if (users.Count > 1)
                {
                    tasks = tasks.Concat(new[] {
                        new TaskRoute.Models.Commission
                        {
                            Title = "Ritirare vestiti dalla lavanderia",
                            Description = "Andare alla lavanderia XYZ.",
                            DueDate = DateTime.Now.AddDays(2),
                            IsCompleted = false,
                            UserId = users[1].Id, // Associa al secondo utente
                            LocationId = dbLocations[2].Id // Associa alla terza posizione
                        }
                    }).ToArray();
                }

                foreach (var task in tasks)
                {
                    context.Commissions.Add(task);
                }
                await context.SaveChangesAsync();
            }
        }
    }
}