#if TEST

#region Using directives

using MbUnit.Core.Framework;
using MbUnit.Framework;

using NGExtension;

#endregion

namespace GraemeF.NewsGator
{
    public partial class FetchLinks : INewsGatorExtension
    {
        [TestFixture]
        public class Tests
        {
            // TODO: A few more tests would be nice! :)

            [Test]
            public void graemefdotcom()
            {
                // just sees if anything is obviously going to fall over
                FetchLinks f = new FetchLinks();
                f.GetContent("http://graemef.com", "http://graemef.com");
            }
        }
    }
}
#endif