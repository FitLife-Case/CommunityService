using Community.Models;

namespace Community.Tests.Models;

[TestClass]
public class PostTests
{
    [TestMethod]
    public void Post_Should_Have_Default_Values()
    {
        // Arrange + Act
        // Opretter et tomt Post objekt for at teste standardværdierne.
        var post = new Post();

        // Assert
        // Tjekker at alle string-felter starter tomme.
        Assert.AreEqual(string.Empty, post.Id);
        Assert.AreEqual(string.Empty, post.AuthorUserId);
        Assert.AreEqual(string.Empty, post.AuthorMemberId);
        Assert.AreEqual(string.Empty, post.AuthorDisplayName);
        Assert.AreEqual(string.Empty, post.Title);
        Assert.AreEqual(string.Empty, post.Content);

        // Tjekker at nullable center-felter starter som null.
        Assert.IsNull(post.AuthorHomeCenterId);
        Assert.IsNull(post.CenterId);

        // Tjekker at scope starter som Global.
        Assert.AreEqual(CommunityScope.Global, post.Scope);

        // Tjekker at posten ikke er slettet som standard.
        Assert.IsFalse(post.IsDeleted);

        // Tjekker at kommentarlisten findes og starter tom.
        Assert.IsNotNull(post.Comments);
        Assert.IsEmpty(post.Comments);
    }

    [TestMethod]
    public void Post_Should_Allow_Global_Post()
    {
        // Arrange + Act
        // Opretter et globalt opslag uden CenterId.
        var post = new Post
        {
            Title = "Global test",
            Content = "Global content",
            Scope = CommunityScope.Global
        };

        // Assert
        // Tjekker at værdierne bliver sat korrekt.
        Assert.AreEqual("Global test", post.Title);
        Assert.AreEqual("Global content", post.Content);
        Assert.AreEqual(CommunityScope.Global, post.Scope);

        // Et globalt opslag skal ikke have CenterId.
        Assert.IsNull(post.CenterId);
    }

    [TestMethod]
    public void Post_Should_Allow_Center_Post()
    {
        // Arrange + Act
        // Opretter et center-opslag med et specifikt CenterId.
        var post = new Post
        {
            Title = "Center test",
            Content = "Center content",
            Scope = CommunityScope.Center,
            CenterId = "22222222-2222-2222-2222-222222222222"
        };

        // Assert
        // Tjekker at center-opslaget har korrekt titel, indhold, scope og CenterId.
        Assert.AreEqual("Center test", post.Title);
        Assert.AreEqual("Center content", post.Content);
        Assert.AreEqual(CommunityScope.Center, post.Scope);
        Assert.AreEqual("22222222-2222-2222-2222-222222222222", post.CenterId);
    }

    [TestMethod]
    public void Post_Should_Allow_Comments()
    {
        // Arrange
        // Opretter et opslag uden kommentarer.
        var post = new Post();

        // Act
        // Tilføjer en kommentar til opslaget.
        post.Comments.Add(new Comment
        {
            AuthorMemberId = "member-1",
            AuthorDisplayName = "Test Member",
            Content = "Test kommentar"
        });

        // Assert
        // Tjekker at kommentaren blev tilføjet korrekt.
        Assert.AreEqual(1, post.Comments.Count);
        Assert.AreEqual("Test kommentar", post.Comments[0].Content);
        Assert.AreEqual("Test Member", post.Comments[0].AuthorDisplayName);
    }
}