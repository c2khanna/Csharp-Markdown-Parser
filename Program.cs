using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace ConsoleApplication4
{
    public class attribute
    {
        public string key { get; set; }
        public string value { get; set; }
    }
    public class fragment
    {
        public string type { get; set; }
        public string content { get; set; }
        public int number { get; set; }
    }

    public class paragraph
    {
        public string type { get; set; }
        public List<fragment> content { get; set; }
    }

    public class tag
    {
        public string name { get; set; }
        public List<object> contents { get; set; }
        public List<attribute> attributes { get; set; }
    }

    public class replacement
    {
        public string symbol { get; set; }
        public string equivalent { get; set; }
    }
    class Program
    {
        static tag link(string target, string text)
        {
            List<object> contents = new List<object>();
            List<attribute> attribute = new List<attribute>();

            contents.Add(text);
            attribute.Add(new attribute { key = "href", value = target });

            return new tag { name = "a", contents = contents, attributes = attribute };
        }

        static tag htmlDoc(string title, List<object> bodyContent)
        {
            List<object> contentsofTitle = new List<object>();
            List<object> contentsofHead = new List<object>();
            //List<object> contentsofBody = new List<object>();
            List<object> contentsofHtml = new List<object>();

            contentsofTitle.Add(title);
            //contentsofBody.Add(bodyContent);

            tag titleTag = new tag { name = "title", contents = contentsofTitle };

            contentsofHead.Add(titleTag);

            tag head = new tag { name = "head", contents = contentsofHead };
            tag body = new tag { name = "body", contents = bodyContent };
            contentsofHtml.Add(head);
            contentsofHtml.Add(body);

            tag html = new tag { name = "html", contents = contentsofHtml };
            return html;
        }

        static tag image(string src)
        {
            List<attribute> attribute = new List<attribute>();
            attribute.Add(new attribute { key = "src", value = src });

            return new tag { name = "img", contents = null, attributes = attribute };
        }

        static tag footnote(int number)
        {
            List<object> contents = new List<object>();
            contents.Add(link("#footnote" + number, number.ToString()));
            return new tag { name = "sup", contents = contents };
        }

        static object renderFragment(fragment frag)
        {
            if (frag.type == "ref")
            {
                return footnote(frag.number);
            }
            else if (frag.type == "emp")
            {
                List<object> con = new List<object>();
                con.Add(frag.content);
                return new tag { name = "em", contents = con };
            }
            else if (frag.type == "normal")
            {
                return frag.content;
            }
            else
                return null;
        }

        static tag renderParagraph(paragraph para)
        {
            return new tag { name = para.type, contents = para.content.Select(renderFragment).ToList() };
        }

        static tag renderFootnote(fragment frag)
        {
            var number = "[" + frag.number + "]";
            tag anchor = new tag();
            anchor.name = "a";

            List<object> con = new List<object>();
            con.Add(number);
            anchor.contents = con;

            List<attribute> attri = new List<attribute>();
            attribute atribute = new attribute();
            atribute.key = "name";
            atribute.value = "footnote" + frag.number;
            attri.Add(atribute);
            anchor.attributes = attri;

            List<object> contentsofp = new List<object>();
            tag sm = new tag();
            sm.name = "small";
            List<object> contentsofSm = new List<object>();
            contentsofSm.Add(anchor);
            contentsofSm.Add(frag.content);
            
            sm.contents = contentsofSm;

            contentsofp.Add(sm);

            return new tag { name = "p", contents = contentsofp };
        }

        static string escapeHTML(string text)
        {
            replacement[] replacements = new replacement[4];

            replacements[0] = new replacement();
            replacements[1] = new replacement();
            replacements[2] = new replacement();
            replacements[3] = new replacement();

            replacements[0].symbol = "&";
            replacements[1].symbol = "\"";
            replacements[2].symbol = "<";
            replacements[3].symbol = ">";

            replacements[0].equivalent = "&amp";
            replacements[1].equivalent = "&quot";
            replacements[2].equivalent = "&lt";
            replacements[3].equivalent = "&gt";

            foreach (replacement rep in replacements)
            {
                text = text.Replace(rep.symbol, rep.equivalent);
            }
            return text;
        }

        static List<fragment> splitParagraph(string paragraph)
        {

            List<fragment> fragments = new List<fragment>();

            Func<char, int> indexOrEnd = null;

            indexOrEnd = new Func<char, int>(ch =>
            {
                int index = paragraph.IndexOf(ch);
                return index == -1 ? paragraph.Length : index;
            });


            Func<char, string> takeUpto = null;

            takeUpto = new Func<char, string>(ch =>
            {
                int end = paragraph.IndexOf(ch, 1);
                if (end == -1)
                    throw new Exception("missing closing '" + ch + "'");
                string part = paragraph.Substring(1, end - 1);
                paragraph = paragraph.Substring(end + 1);
                return part;
            });

            Func<string> takeNormal = () =>
            {
                var str = new char[] { '*', '{' }.Select(x => indexOrEnd(x));
                var endof = str.Aggregate(paragraph.Length, Math.Min);
                string part = paragraph.Substring(0, endof);
                paragraph = paragraph.Substring(endof);
                return part;
            };

            while (paragraph != "")
            {
                if (paragraph[0] == '*')
                {
                    fragments.Add(new fragment { type = "emp", content = takeUpto('*') });
                }
                else if (paragraph[0] == '{')
                {
                    fragments.Add(new fragment { type = "footnote", content = takeUpto('}') });
                }
                else
                    fragments.Add(new fragment { type = "normal", content = takeNormal() });

            }
            return fragments;
        }

        static void render(object ele, Func<List<attribute>, string> renderAttributes, List<string> pieces)
        {
            if (ele is string)
            {
                string st = (string)ele;
                pieces.Add(escapeHTML(st));
            }
            else if (ele is tag)
            {
                tag t = (tag)ele;
                if (t.contents.Count == 0)
                {
                    pieces.Add("<" + t.name + " " + renderAttributes(t.attributes) + "/>");
                }
                else
                {
                    pieces.Add("<" + t.name + " " + renderAttributes(t.attributes) + ">");

                    foreach (object con in t.contents)
                    {
                        render(con, renderAttributes, pieces);
                    }
                    pieces.Add("</" + t.name + ">");
                }
                
            }
        }

        static string renderHTML(tag element)
        {
            List<string> pieces = new List<string>();
            
            Func<List<attribute>, string> renderAttributes = null;

            renderAttributes = new Func<List<attribute>, string>(attribute =>
            {
                string res = string.Empty;

                if (attribute != null)
                {
                    List<string> result = new List<string>();

                    foreach (attribute atri in attribute)
                    {
                        if (atri != null)
                        {
                            if (atri.key != null)
                            {
                                result.Add(atri.key + " =\"" + escapeHTML(atri.value) + "\"");
                            }
                        }
                    }

                    res = string.Join("", result.ToArray());
                }

                return res;
            });

            render(element, renderAttributes, pieces);

            string str = string.Join("", pieces.ToArray());

            return str;
        }

        static paragraph processParagraph(string paragraph)
        {
            paragraph para = new paragraph();
            int header = 0;

            while (paragraph[0] == '%')
            {

                paragraph = paragraph.Substring(1, paragraph.Length - 1);
                header++;

            }
            para.type = (header == 0 ? "p" : "h" + header);
            para.content = splitParagraph(paragraph);
            return para;
        }

        static List<fragment> extractFootnotes(List<paragraph> paras)
        {
            List<fragment> footnotes = new List<fragment>();
            int currentnote = 0;

            Func<fragment, fragment> replaceFootnote = null;

            replaceFootnote = new Func<fragment, fragment>(fragments =>
            {
                if (fragments.type == "footnote")
                {
                    currentnote++;
                    footnotes.Add(fragments);
                    fragments.number = currentnote;
                    return new fragment { type = "ref", number = currentnote };
                }
                else
                {
                    return fragments;
                }
            });

            foreach (paragraph para in paras)
            {
                para.content = para.content.Select(ch => replaceFootnote(ch)).ToList();
            }

            return footnotes;
        }

        static string renderFile(string file, string title)
        {
            string[] paragraphs = file.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.None);

            for (int i = 0; i < paragraphs.Length; i++)
            {
                paragraphs[i] = paragraphs[i].Replace("\r\n", " ");
            }

            List<paragraph> paras = new List<paragraph>();

            for (int i = 0; i < paragraphs.Length; i++)
            {
                paras.Add(processParagraph(paragraphs[i]));

            }

            var footnotes = extractFootnotes(paras).Select(renderFootnote);

            var body = paras.Select(renderParagraph).Concat(footnotes).ToList();
            //var body = paras.Select(renderParagraph);//.Concat(footnotes);

            var b = body.Select(bb => (object)bb).ToList();


            return renderHTML(htmlDoc(title, b));
        }

        static void Main(string[] args)
        {
            string fileName = "C:\\Users\\chaitanya.khanna\\Desktop\\Data.txt";
            StreamReader readFile = new StreamReader(fileName);
            string Data = readFile.ReadToEnd();

            string htmlD = renderFile(Data, "The book of Programming");

            File.WriteAllText("D:/chaitanya.html", htmlD);


            Console.Read();
        }
    }

}
