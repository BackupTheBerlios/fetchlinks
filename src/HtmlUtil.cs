#region Using directives

using System;
using System.Net;
using System.IO;
using System.Text;

using HtmlAgilityPack;

#endregion

namespace GraemeF.NewsGator
{
    public abstract partial class HtmlUtil
    {
        /// <summary>
        /// Logger for the FetchLinks class.
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(HtmlUtil));

        /// <summary>
        /// Decodes a response from a web server, using any encodings specified in the response headers or body.
        /// </summary>
        /// <param name="w">Response from a web server.</param>
        /// <returns>Decoded body from the response.</returns>
        /// <remarks>This function is supplied by Feroze Daud. See http://blogs.msdn.com/feroze_daud/archive/2004/03/30/104440.aspx for details.</remarks>
        public static String DecodeData(WebResponse w)
        {
            //
            // first see if content length header has charset = calue
            //
            String charset = null;
            String ctype = w.Headers["content-type"];
            if (ctype != null)
            {
                int ind = ctype.IndexOf("charset=");
                if (ind != -1)
                {
                    charset = ctype.Substring(ind + 8);
                    log.Debug("CT: charset=" + charset);
                }
            }

            // save data to a memorystream
            MemoryStream rawdata = new MemoryStream();
            byte[] buffer = new byte[1024];
            Stream rs = w.GetResponseStream();
            int read = rs.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                rawdata.Write(buffer, 0, read);
                read = rs.Read(buffer, 0, buffer.Length);
            }

            rs.Close();

            //
            // if ContentType is null, or did not contain charset, we search in body
            //
            if (charset == null)
            {
                MemoryStream ms = rawdata;
                ms.Seek(0, SeekOrigin.Begin);

                StreamReader srr = new StreamReader(ms, Encoding.ASCII);
                String meta = srr.ReadToEnd();

                if (meta != null)
                {
                    int start_ind = meta.IndexOf("charset=");
                    int end_ind = -1;
                    if (start_ind != -1)
                    {
                        end_ind = meta.IndexOf("\"", start_ind);
                        if (end_ind != -1)
                        {
                            int start = start_ind + 8;
                            charset = meta.Substring(start, end_ind - start + 1);
                            charset = charset.TrimEnd(new Char[] { '>', '"' });
                            log.Debug("META: charset=" + charset);
                        }
                    }
                }
            }

            Encoding e = null;
            if (charset == null)
            {
                e = Encoding.ASCII; //default encoding
            }
            else
            {
                try
                {
                    e = Encoding.GetEncoding(charset);
                }
                catch (Exception ee)
                {
                    log.Warn("Exception: GetEncoding: " + charset, ee);
                    e = Encoding.ASCII;
                }
            }

            rawdata.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(rawdata, e);

            String s = sr.ReadToEnd();

            return s;
        }

        /// <summary>
        /// Replaces the base href for some HTML supplied in a string.
        /// </summary>
        /// <param name="content">HTML content to have its base href replaced.</param>
        /// <param name="baseUri">New URI for the base href.</param>
        /// <returns>The content with a base element pointing at baseUri.</returns>
        public static string SetBaseUri(string content, string baseUri)
        {
            // Quick and dirty method: Just do a search and replace on a <head> element.
            //            if (content.IndexOf("<head>") >= 0)
            //                return content.Replace("<head>", String.Format("<head><base href=\"{0}\" />", response.ResponseUri.AbsoluteUri));
            //
            //            if (content.IndexOf("<HEAD>") >= 0)
            //                return content.Replace("<HEAD>", String.Format("<HEAD><base href=\"{0}\" />", response.ResponseUri.AbsoluteUri));

            // Load the content into an HTML document
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);

            // find the head element (if there is one)

            // first, look for /html/head
            HtmlNode head = doc.DocumentNode.SelectSingleNode("/html/head");
            if (head == null)
                // now look for /head
                head = doc.DocumentNode.SelectSingleNode("/head");

            if (head == null)
            {
                // still no head element - create one and insert it
                head = doc.CreateElement("head");
                doc.DocumentNode.PrependChild(head);
            }

            if (head != null)
            {
                // look for a base element in the head
                HtmlNode baseElement = head.SelectSingleNode("base");
                if (baseElement == null)
                {
                    // not there - create one and insert
                    baseElement = doc.CreateElement("base");
                    head.PrependChild(baseElement);
                }

                // set the base element's href attribute to the new baseUri
                baseElement.SetAttributeValue("href", baseUri);
            }
            else
                log.Warn("Could not find or create a head element.");

            // convert the document back to a string and return
            return doc.DocumentNode.OuterHtml;
        }

    }
}
