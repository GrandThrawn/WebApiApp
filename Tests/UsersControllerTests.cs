using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApiApp.Controllers;
using WebApiApp.Data;
using WebApiApp.Models;
using Xunit;

namespace WebApiApp.Tests
{
    public class UserControllerTests
    {
        private readonly UserController _controller;
        private readonly AppDbContext _context;

        public UserControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new AppDbContext(options);
            _controller = new UserController(_context);

            // Установка контекста для контроллера
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, "TestUser ") }))
                }
            };
        }

        [Fact]
        public async Task GetAllUsers_ReturnsListOfUsers()
        {
            // Arrange
            var user1 = new User { Id = Guid.NewGuid(), Name = "User  1", Email = "user1@example.com" };
            var user2 = new User { Id = Guid.NewGuid(), Name = "User  2", Email = "user2@example.com" };
            await _controller.CreateUser(user1);
            await _controller.CreateUser(user2);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<ActionResult<IEnumerable<User>>>(result);
            var users = Assert.IsAssignableFrom<IEnumerable<User>>(okResult.Value);
            Assert.Equal(2, users.Count());
        }

        [Fact]
        public async Task GetUserById_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Name = "User ", Email = "user@example.com" };
            await _controller.CreateUser(user);

            // Act
            var result = await _controller.GetUserById(user.Id);

            // Assert
            var okResult = Assert.IsType<ActionResult<User>>(result);
            var returnedUser = Assert.IsType<User>(okResult.Value);
            Assert.Equal(user.Name, returnedUser.Name);
        }

        [Fact]
        public async Task CreateUser_ReturnsCreatedResult_WhenUserIsValid()
        {
            // Arrange
            var newUser = new User { Id = Guid.NewGuid(), Name = "New User", Email = "newuser@example.com" };

            // Act
            var result = await _controller.CreateUser(newUser);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedUser = Assert.IsType<User>(createdResult.Value);
            Assert.Equal(newUser.Name, returnedUser.Name);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenUserIsUpdated()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Name = "User ", Email = "user@example.com" };
            await _controller.CreateUser(user);
            var updatedUser = new User { Name = "Updated User", Email = "updateduser@example.com" };

            // Act
            var result = await _controller.UpdateUser(user.Id, updatedUser);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNoContent_WhenUserIsDeleted()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Name = "User ", Email = "user@example.com" };
            await _controller.CreateUser(user);

            // Act
            var result = await _controller.DeleteUser(user.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}