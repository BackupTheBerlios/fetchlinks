#if TEST

#region Using directives

using System.Collections;

using MbUnit.Core.Framework;
using MbUnit.Framework;

using HtmlAgilityPack;

#endregion

namespace GraemeF.NewsGator
{
    public abstract partial class HtmlUtil
    {
        [TestFixture]
        public class Tests
        {
            [Factory(typeof(string))]
            public IEnumerable TestHtmlContent()
            {
                // simple existing
                yield return "<head><base href='boo' /></head><body><h1>Hello</h1>";

                // mixed caps
                yield return "<hEaD><BaSe hREF='boo' /></HeAd><BOdY><h1>Hello</h1>";

                // missing head
                yield return "<h1>Hello</h1>";

                // head, but no base
                yield return "<head/><h1>Hello</h1>";

                // head and base but no href
                yield return "<head><base/></head><h1>Hello</h1>";

                // real XHTML
                yield return @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Transitional//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd""><html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en""><head>  <title>graemef.com | there's nothing quite like whinging in public</title>  <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" /><base href=""http://notreally.com/"" /><style type=""text/css"" media=""all"">@import url(misc/drupal.css);</style><link rel=""alternate"" type=""application/atom+xml"" title=""Atom"" href=""?q=atom/feed"" /><style type=""text/css"">@import url(http://graemef.com/modules/project/project.css);</style><link rel=""alternate"" type=""application/rss+xml"" title=""RSS"" href=""?q=node/feed"" />  <style type=""text/css"" media=""all"">@import ""themes/xtemplate/pushbutton/xtemplate.css"";</style></head>";
            }

            [CombinatorialTest(Description = "Makes sure that the base href can be set on many different inputs.")]
            public void SetBaseHref([UsingFactories("TestHtmlContent")] string test)
            {
                // set the new base href on the test input
                string result = SetBaseUri(test, "http://graemef.com");

                // load the resulting html into a document
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);

                // make sure the base href is the value we set it to
                Assert.AreEqual("http://graemef.com", doc.DocumentNode.SelectSingleNode("//head/base").GetAttributeValue("href", null));
            }
        }
    }
}
#endif