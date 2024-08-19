﻿using my_books.Data.Model;

namespace my_books.Data
{
    public class AppDbInitializer
    {
        public static void Seed(IApplicationBuilder applicationBuilder) 
        {
            using (var seviceScope=applicationBuilder.ApplicationServices.CreateScope()) 
            {
                var context= seviceScope.ServiceProvider.GetService<AppDbContext>();
                if (!context.Books.Any())
                {
                    context.Books.AddRange(new Book()
                    {
                        Title = "1st Book Title",
                        Description = "1st Book Description",
                        IsRead = true,
                        DateRead = DateTime.Now.AddDays(-10),
                        Rate = 4,
                        Genre = "Biography",
                        CoverUrl = "https....",
                        DateAdded = DateTime.Now
                    },
                    new Book
                    {
                        Title = "2nd Book Title",
                        Description = "2nd Book Description",
                        IsRead = false,
                        Genre = "Biography",
                        CoverUrl = "https....",
                        DateAdded = DateTime.Now
                    });
                    context.SaveChanges();
                }
            }
        
        }
    }
}
