using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;


namespace TemplateParser
{
    public class TemplateEngineImpl : ITemplateEngine
    {

        ArrayList info = new ArrayList();


        /// <summary>
        /// Applies the specified datasource to a string template, and returns a result string
        /// with substituted values.
        /// 
        /// Prerequest:
        /// Template and dataSource not Null
        /// 
        /// </summary>
        public string Apply(string template, object dataSource)
        {
            //TODO: Write your implementation here that passes all tests in TemplateParser.Test project            
            //throw new NotImplementedException();

            if(template.Equals(null) || dataSource.Equals(null))
            {
                throw new System.ArgumentException("Parameters cannot be null");
            }

            //Check which template is parsering Console.WriteLine(template);
            this.parsingTemplate(template,dataSource);

            StringBuilder retStrBuilder = new StringBuilder();
            
            foreach(String str in this.info)
            {
                retStrBuilder.Append(str + " ");
            }
            
            String retStr = retStrBuilder.ToString().Trim();
            //Check the final result Console.WriteLine(retStr);
            return retStr;
        }

        //==================================================================== parsering methods ====================================================================

        /// <summary>
        /// The main parser tree framework.
        /// 
        /// Pre request:
        /// Both template and datasource cannot be null, otherwise throw null exception
        /// 
        /// Normal case:
        /// conver template to string list and parse each string into result array list.
        /// if it was included in "[]" then parsing it as a property, otherwise it would be content string.
        /// 
        /// Special cases:
        /// These cases cannot conver to string list directly, because it might exists nested cases.
        /// The program should be able to parse infinite nested level,which by using recursive method.
        /// for the innerst level could regard it as normal case.
        /// 
        /// With Token: [with] [/with]
        /// 
        /// Date Token: "d MMMM yyy"
        /// If the template contain date token then replace token by 'time' and delete the blank space.
        /// After replace procedure, the template could treat as normal case.
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dataSource"></param>
        private void parsingTemplate(String template, object dataSource)
        {
            if (template.StartsWith("[with"))
            {
                int end = this.findLastSlashWith(template);

                if(end >= 0)
                {
                    int endBreakIndex = this.findTokenStartIndex(template, "]");
                    String substitutionOfscoped = template.Substring(6, endBreakIndex - 6);

                    String content = template.Substring(endBreakIndex + 1, end - endBreakIndex - 7);

                    var scopedProp = dataSource.GetType().GetProperty(substitutionOfscoped).GetValue(dataSource, null);

                    if (content.Contains("[with"))
                    {

                        int nextWithIndex = this.findTokenStartIndex(content, "[with"); 

                       String noWithContent = content.Substring(0, nextWithIndex);
                       String nextWithContent = content.Substring(nextWithIndex);
                       this.parsingTemplate(noWithContent, scopedProp);
                       this.parsingTemplate(nextWithContent, scopedProp);
               
                    }
                    else
                    {
                        this.parsingTemplate(content, scopedProp);
                    }
                }
                else
                {
                    throw new TokenNotFoundException("found [with but have not fond token [/with]");
                }

            }
            else if(template.Contains("d MMMM yyyy"))
            {
                String content = this.replaceTimeToken(template);
                this.parsingTemplate(content, dataSource);
             
            }
            else
            {
                String[] result = template.Trim().Split(' ');
                foreach (String s in result)
                {
                    //no [ won't be parsed 
                    if (s.StartsWith("["))
                    {
                        String propValue = this.parsingProperty(s, dataSource);

                        this.info.Add(propValue);
                    }
                    else
                    {
                        this.info.Add(s);
                    }
                }

            }
           
        }

        /// <summary>
        /// If the string included in "[]" then treat it as a property. 
        /// Getting the end "]" character index to get the property name,
        /// if the dataSource contain the property name then get its value.
        /// Otherwise replace with a blank string.
        /// 
        /// Special Cases:
        /// substitution of spanned ([object.property]):
        /// call method parseSubstituition
        /// 
        /// contain time:
        /// token of /time
        /// call method parseTime
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        private String parsingProperty(String content, object dataSource)
        {
            String ret = "";
            if (content.Contains('.'))
            {
               ret = this.parseSubstitution(content, dataSource);
            }
            else if (content.Contains("/time"))
            {
                ret = this.parseTime(content, dataSource);
            }
            else
            {
                int end = content.IndexOf("]");
                if (end >= 0)
                {
                    String property;

                    if (content.StartsWith("["))
                    {
                        property = content.Substring(1, end - 1);
                    }
                    else
                    {
                        property = content.Substring(0, end);
                    }
                   
                    if(dataSource.GetType().GetProperty(property) != null)
                    {
                        ret = dataSource.GetType().GetProperty(property).GetValue(dataSource, null).ToString();
                    } 
                    
                }
                else
                {
                    throw new TokenNotFoundException("have not fond token ]");
                }
            }

            return ret;

         }


        /// <summary>
        /// In the schema of substitution of spanned ([object.property])
        /// find its anonymous name at "." left side and get it corresponding value which at "." right side
        /// </summary>
        /// <param name="content"></param>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        private string parseSubstitution(String content, object dataSource)
        {
            String ret = "";
            int end = content.IndexOf(".");
            if (end >= 0)
            {
                String spanned = content.Substring(1, end - 1);
                var spanProp = dataSource.GetType().GetProperty(spanned).GetValue(dataSource, null);
  
                String remainder = content.Substring(end + 1);
                ret = parsingProperty(remainder, spanProp);
                
            }
            else
            {
                throw new TokenNotFoundException("have not fond token . ");
            }

            return ret;
        }

        /// <summary>
        /// It needs to get property name which located before /time token.
        /// For the property then find its type, if it equals to DateTimeOff then cast it to get the value.
        /// Otherwise treat it as a normal property
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        private String parseTime(String content, object dataSource)
        {
            String ret = "";

            int end = findTokenStartIndex(content, "/time]");
            if (end >= 0)
            {
                String timeProp = content.Substring(1, end - 1);
                PropertyInfo propInfo = dataSource.GetType().GetProperty(timeProp);

                if (dataSource.GetType().GetProperty(timeProp).PropertyType == typeof(DateTimeOffset))
                {
                    DateTimeOffset test = (DateTimeOffset)dataSource.GetType().GetProperty(timeProp).GetValue(dataSource, null);
                    ret = test.DateTime.ToString("d MMMM yyyy");
                }
                else
                {
                    ret = dataSource.GetType().GetProperty(timeProp).GetValue(dataSource, null).ToString();
                }
            }
            else
            {
               throw new TokenNotFoundException("have not fond token /time ");
            }

            return ret;
        }


        //==================================================================== help methods ====================================================================

        /// <summary>
        /// In this Case, it is different, because we need find the content between first and last with token.
        /// It exists nested pattern, so that if do the string matching then if need cost much than this algorithm.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private int findLastSlashWith(String content)
        {
           
            int end = content.LastIndexOf("]");
            if (end >= 0 )
            {
                if(content[end - 5].Equals("/")  && content[end - 6].Equals("["))
                {
                    end = end - 6;
                }

            }

            return end;
        }

        /// <summary>
        /// The main idea here is String matching, The algorithm that I choose here is Brute Force, due to here is the normal cases,
        /// It could using KMP Algorithm in the futher for advance.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="token"></param>
        /// <returns> Return the first matching string</returns>
        private int findTokenStartIndex(String content, String token)
        {
            int count = 0;
            int length = token.Length;
            do
            {
                count++;
               
            }
            while (!content.Substring(count, length).Equals(token));

            return count;
        }

        /// <summary>
        /// Replace the date token by "time" and delete its white blank space
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private String replaceTimeToken(String content)
        {
            int beforeDateIndex = this.findTokenStartIndex(content, "d MMMM yyyy");
            String newContent = content.Substring(0, beforeDateIndex - 1).Trim() + "/time" + content.Substring(beforeDateIndex + 12);
          
            if (newContent.Contains("d MMMM yyyy"))
            {
                newContent = this.replaceTimeToken(newContent);
            }

            return newContent;

        }

    }
}

