using Community.Models;

namespace Community.Tests.Models;

[TestClass]
public class PostTests
{
    [TestMethod]
    public void Post_Should_Have_Default_Values()
    {
        var post = new Post();

        Assert.IsFalse(post.IsDeleted);

        Assert.IsNotNull(post.Comments);

        Assert.AreEqual(0, post.Comments.Count);
    }
}