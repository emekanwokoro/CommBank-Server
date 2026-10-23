using CommBank.Controllers;
using CommBank.Services;
using CommBank.Models;
using CommBank.Tests.Fake;
using Microsoft.AspNetCore.Mvc;

namespace CommBank.Tests;

public class GoalControllerTests
{
    private readonly FakeCollections collections;

    public GoalControllerTests()
    {
        collections = new();
    }

    [Fact]
    public async Task GetAll()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        IGoalsService goalsService = new FakeGoalsService(goals, goals[0]);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.Get();

        // Assert
        var index = 0;
        foreach (Goal goal in result)
        {
            Assert.IsAssignableFrom<Goal>(goal);
            Assert.Equal(goals[index].Id, goal.Id);
            Assert.Equal(goals[index].Name, goal.Name);
            index++;
        }
    }

    [Fact]
    public async Task Get()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        IGoalsService goalsService = new FakeGoalsService(goals, goals[0]);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.Get(goals[0].Id!);

        // Assert
        Assert.IsAssignableFrom<Goal>(result.Value);
        Assert.Equal(goals[0], result.Value);
        Assert.NotEqual(goals[1], result.Value);
    }

    [Fact]
    public async Task GetForUser()
    {
        // Arrange
        const string userId = "62a29c15f4605c4c9fa7f306";
        const string otherUserId = "62a29c15f4605c4c9fa7f307";
        var goals = collections.GetGoals();
        goals[0].UserId = userId;
        goals[0].Icon = "🤺";
        goals[1].UserId = userId;
        goals[1].Icon = null;
        goals[2].UserId = otherUserId;
        goals[2].Icon = "✈️";

        var users = collections.GetUsers();
        var goalsService = new UserGoalsService(goals);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var result = await controller.GetForUser(userId);

        // Assert
        Assert.Equal(userId, goalsService.RequestedUserId);
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.All(result, goal =>
        {
            Assert.IsAssignableFrom<Goal>(goal);
            Assert.Equal(userId, goal.UserId);
        });
        Assert.Contains(result, goal => goal.Id == goals[0].Id && goal.Icon == "🤺");
        Assert.Contains(result, goal => goal.Id == goals[1].Id && goal.Icon == null);
        Assert.DoesNotContain(result, goal => goal.UserId == otherUserId);
    }

    [Fact]
    public async Task GetForUser_WhenUserHasNoGoals_ReturnsEmptyList()
    {
        // Arrange
        const string userId = "62a29c15f4605c4c9fa7f306";
        const string otherUserId = "62a29c15f4605c4c9fa7f307";
        var goals = collections.GetGoals();
        foreach (var goal in goals)
        {
            goal.UserId = otherUserId;
        }

        var users = collections.GetUsers();
        var goalsService = new UserGoalsService(goals);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var result = await controller.GetForUser(userId);

        // Assert
        Assert.Equal(userId, goalsService.RequestedUserId);
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    private sealed class UserGoalsService : IGoalsService
    {
        private readonly List<Goal> goals;

        public UserGoalsService(List<Goal> goals)
        {
            this.goals = goals;
        }

        public string? RequestedUserId { get; private set; }

        public Task<List<Goal>?> GetForUserAsync(string id)
        {
            RequestedUserId = id;
            return Task.FromResult<List<Goal>?>(goals.Where(goal => goal.UserId == id).ToList());
        }

        public Task<List<Goal>> GetAsync() => throw new NotSupportedException();
        public Task<Goal?> GetAsync(string id) => throw new NotSupportedException();
        public Task CreateAsync(Goal newGoal) => throw new NotSupportedException();
        public Task UpdateAsync(string id, Goal updatedGoal) => throw new NotSupportedException();
        public Task RemoveAsync(string id) => throw new NotSupportedException();
    }
}
