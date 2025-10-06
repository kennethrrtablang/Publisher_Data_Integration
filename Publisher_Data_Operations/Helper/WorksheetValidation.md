<ol style="box-sizing: border-box; padding-left: 40px; color: rgb(0, 0, 0); font-family: &quot;Segoe UI VSS (Regular)&quot;, &quot;Segoe UI&quot;, -apple-system, BlinkMacSystemFont, Roboto, &quot;Helvetica Neue&quot;, Helvetica, Ubuntu, Arial, sans-serif, &quot;Apple Color Emoji&quot;, &quot;Segoe UI Emoji&quot;, &quot;Segoe UI Symbol&quot;; font-size: 14px; font-style: normal; font-variant-ligatures: normal; font-variant-caps: normal; font-weight: 400; letter-spacing: normal; orphans: 2; text-align: start; text-indent: 0px; text-transform: none; white-space: normal; widows: 2; word-spacing: 0px; -webkit-text-stroke-width: 0px; background-color: rgb(255, 255, 255); text-decoration-thickness: initial; text-decoration-style: initial; text-decoration-color: initial;">
    <li style="box-sizing: border-box; list-style: inherit;"><b style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Validation Level 1:</span></font><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></b><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;Identify data file based on file naming convention: if pass set IsValidFile = True (1) in &quot;pdi_file_receipt_log&quot; table, else set false (0)</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
        <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Does the data file name adhere to the expected naming convention.&nbsp;Eight parameters separated by an underscore &quot;_&quot;</span></font></span><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;DataCustodian&gt;</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;ClientName&gt;</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;LOB&gt;</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;DataType&gt;</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;DocumentType&gt;</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;YYYYMMDD&gt; (file creation date)</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;HHMMSS&gt; (upload timestamp)</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&lt;Version #&gt;</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">File extension = .xlsx</span></font></li>
            </ol>
        </li>
        <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check the Data File Log table if existing record of this file</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">if duplicate filename - flag error</span></font></li>
            </ol>
        </li>
        </ol>
    </li>
    <li style="box-sizing: border-box; list-style: inherit;"><b style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Validation Level 2 part a:</span></font><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></b><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;confirm data file structure aligns with expected specification: (Note both Validation level 2 part a &amp; b (separate user story) needed to qualify if pass set IsValidData = True (1) in &quot;pdi_file_log&quot; table, else set false (0)</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
        <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Run full Fund Fact BAU data File validation check</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Name of the following should be included in the data file</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: decimal;">
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Macros</span></font></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Formulas</span></font></li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm the following:</span></font></span><ol style="box-sizing: border-box; padding-left: 40px; list-style: decimal;">
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check tab names &quot;Document Data&quot;, &quot;FF16&quot;, &quot;FF17&quot;, &quot;FF40&quot;</span></font><br style="box-sizing: border-box;" />
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Header Rows</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
                    <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&quot;DocumentData&quot; tab - rows 1 and 2</span></font></li>
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; display: inline !important;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&quot;E16&quot;, &quot;E17&quot;, &quot;E40&quot; tabs - row 1</span></font></span><br style="box-sizing: border-box;" />
                    </li>
                    </ol>
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">​All Columns set to &quot;Text Data Type&quot;</span></font></span></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Hidden sheets (none)</span></font></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Filtering (none)</span></font></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Hidden columns / rows (none)</span></font></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check &quot;DocumentData&quot; tab records for blank cells, no blank cells are allowed</span></font></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check&nbsp;</span><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></span></font><span style="box-sizing: border-box; display: inline !important;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&quot;E16&quot;, &quot;E17&quot;, &quot;E40&quot; tabs column A &quot;FundCode&quot; have at least 1 corresponding record on &quot;DocumentData&quot; tab in column E</span></font></span></li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">DocumentData Tab:</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: decimal;">
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm &quot;ClientAccount&quot;, &quot;DocumentType&quot;, and &quot;LineOfBusiness&quot; in columns A / B ensure match against file name and known client account / document type.</span></font><br style="box-sizing: border-box;" />
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Count # of records on &quot;DocumentData&quot; tab start at cell C4</span></font><br style="box-sizing: border-box;" />
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm Date format, check the following columns for correct format DD / MM / YYYY</span></font><ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">F-J, AC, AH, AK, AL,</span></font></li>
                    </ol>
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check that no value has more than 2 decimal places in the following columns:</span></font></span><ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">QZ,&nbsp;</span></font></span><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></span><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">AA, AD, AF, AI, AM-AR, AV-AZ</span></font></li>
                    </ol>
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check that the following values ​​are whole numbers with no decimal places:</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
                    <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">AB, AE, AG, AJ, AS-AU</span></font></li>
                    </ol>
                </li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check that no values ​​have dollar &quot;$&quot; of Percent &quot;%&quot; or comma delimiters &quot;,&quot; only exceptions are columns M / N on &quot;DocumentData&quot; tab.</span></font><ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
                    <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Columns: Q-AB, AD-AG, AI-AJ</span><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></span></font><span style="box-sizing: border-box; display: inline !important;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">, AM-AR, AV-AZ</span></font></span></li>
                    </ol>
                </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm Client Account, and Document Type in columns A / B ensure match against file name and known client account / document type. (duplicate of 2AIII1)</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm all Identifiers in Columns C and BA are Unique</span></font></li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm data structures in&nbsp;&nbsp;</span></font><span style="box-sizing: border-box; display: inline !important;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&quot;E16&quot;, &quot;E17&quot;, &quot;E40&quot; tabs</span></font></span><ol style="box-sizing: border-box; padding-left: 40px;">
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm column B row count resets to 1 for the first row of each unique identifier</span></font></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Confirm Row level indicates only 1 or 2 on&nbsp;</span><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></span></font><span style="box-sizing: border-box; display: inline !important;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&quot;E17&quot;, &quot;E40&quot; tabs</span></font></span></li>
                <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">If Proforma flag = &quot;Y&quot; then&nbsp;</span><span style="box-sizing: border-box; background-color: rgb(255, 255, 0);"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></span><span style="box-sizing: border-box; display: inline !important;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&quot;E16&quot;, &quot;E17&quot;, &quot;E40&quot; tabs should be blank only header rows</span></span></font><span style="box-sizing: border-box; background-color: rgb(255, 255, 255); display: inline !important;">&nbsp;</span></li>
                <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; display: inline !important;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Ensure only unique identifiers listed on &quot;DocumentData&quot; are included. (Column A vs Column E &lt;FundCode&gt;)</span></font></span></li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Count # of records on &quot;DocumentData&quot; tab start at cell C4 (Duplicate of 2AIII2)</span></font><br style="box-sizing: border-box;" />
            </li>
            </ol>
        </li>
        <li style="box-sizing: border-box; list-style: inherit;"><font style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Log file validation check errors for each item in the &quot;pdi_file_validation_log&quot; table</span></font></li>
        </ol>
    </li>
    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);"><b style="box-sizing: border-box;">Validation Level 2 part b:&nbsp;</b>Confirm that only necessary data provided.</span><br style="box-sizing: border-box;" />
        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">For &quot;DocumentCode&quot; belonging to a &lt;FundCode&gt; with data of E17&nbsp;or&nbsp;E40 tab respectively</span><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check Investment Mix Chart 1 (0)&nbsp;and&nbsp;2 Label(s) are present (for each respective record E17sh or E40sh is included to match E17 and E40 data)</span><br style="box-sizing: border-box;" />
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">For &quot;DocumentCode&quot; belonging to a &lt;FundCode&gt; with data of E16</span><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check that columns AA-AB &lt;&gt; &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                    </li>
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check that columns M-N &lt;&gt; &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check that all distinct &quot;FundCode&quot; listed on<span>&nbsp;</span><span style="box-sizing: border-box; display: inline !important;">E16</span>&nbsp;tab are also on E17 tab&nbsp;(E40 is optional)</span><br style="box-sizing: border-box;" />
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check if &quot;InceptionDate&quot; = &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">If True check that &quot;FirstOfferingDate&quot; &lt;&gt; &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check if DocumentCode =&gt; 12 consecutive months (365 days) {column F or G starting date and end date - DataAsAtDate }</span><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">IF true&nbsp;</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">columns AD-AE, AS-AY should not be &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                            </li>
                        </ol>
                    </li>
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Else</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">&nbsp;</span></span><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">columns AD-AE should be &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                            </li>
                        </ol>
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check if&nbsp;DocumentCode =&gt; 1 calendar year</span><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);"><span>&nbsp;</span>(difference between InceptionDate and DataAsAtDate&nbsp;</span><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);"><span style="box-sizing: border-box; background-color: rgb(128, 255, 128); display: inline !important;">(or FirstOfferingDate if InceptionDate is N/A)</span></span><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);">&nbsp;<strike style="box-sizing: border-box;">(</strike></span><strike style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(128, 255, 128);">currentYearMinusOneYear &lt;&gt; N/A)</span></strike><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">IF true&nbsp;</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">columns Q-Z should have at least 1 non &quot;N/A&quot; value in column z</span><br style="box-sizing: border-box;" />
                                <ol style="box-sizing: border-box; padding-left: 40px; list-style: decimal;">
                                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Count number of values that are not &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                                    </li>
                                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Calculate number of full calendar years between &quot;InceptionDate&quot; and &quot;DataAsAtDate&quot;</span><br style="box-sizing: border-box;" />
                                    </li>
                                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">result from a should be equal to b</span><br style="box-sizing: border-box;" />
                                    </li>
                                </ol>
                            </li>
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">columns AF-AK should not be &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                            </li>
                        </ol>
                    </li>
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Else</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box;"><span style="box-sizing: border-box; background-color: rgb(128, 255, 255);">&nbsp;</span></span><span style="box-sizing: border-box; background-color: rgb(128, 255, 255);"><strike style="box-sizing: border-box;">columns AD-AE should not be &quot;N/A&quot;</strike></span><br style="box-sizing: border-box;" />
                            </li>
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(128, 255, 255);">Columns Q-Z should be &quot;N/A&quot;</span></li>
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(128, 255, 255);">Columns AF-AK should be &quot;N/A&quot;</span></li>
                        </ol>
                    </li>
                    <li style="box-sizing: border-box; list-style: inherit;">
                        <br style="box-sizing: border-box;" />
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check if &quot;MerDate &lt;&gt; &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">IF True</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check if FilingDate - MerDate &gt;= 60 days AND&nbsp;</span><span style="box-sizing: border-box; background-color: rgb(128, 255, 255);">Check if MerDate is &gt;= 60 days from the<span>&nbsp;</span>InceptionDate (or FirstOfferingDate if InceptionDate is N/A)</span><br style="box-sizing: border-box;" />
                                <ol style="box-sizing: border-box; padding-left: 40px; list-style: decimal;">
                                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">IF True<span>&nbsp;</span></span><span style="box-sizing: border-box; background-color: rgb(128, 255, 255);">(Must pass both criteria above before these checks)&nbsp;</span><br style="box-sizing: border-box;" />
                                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
                                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AM-AP &lt;&gt; &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                                            </li>
                                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AO = column AM + column AN</span><br style="box-sizing: border-box;" />
                                            </li>
                                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AP = column AO * 100</span><br style="box-sizing: border-box;" />
                                            </li>
                                        </ol>
                                    </li>
                                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Else</span><br style="box-sizing: border-box;" />
                                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-alpha;">
                                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AM-AP = &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                                            </li>
                                        </ol>
                                    </li>
                                </ol>
                            </li>
                        </ol>
                    </li>
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Else</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AQ-AR &lt;&gt; &quot;N/A&quot;</span><br style="box-sizing: border-box;" />
                            </li>
                        </ol>
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check if &quot;PerformanceReset&quot; &lt;&gt; &quot;N/A&quot; (column BH)&nbsp;</span><br style="box-sizing: border-box;" />
                <ol style="box-sizing: border-box; padding-left: 40px;">
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">IF True</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AC &lt;&gt; &quot;N/A&quot;&nbsp;</span><br style="box-sizing: border-box;" />
                            </li>
                        </ol>
                    </li>
                    <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">ELSE</span><br style="box-sizing: border-box;" />
                        <ol style="box-sizing: border-box; padding-left: 40px; list-style: lower-roman;">
                            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">AC = &quot;N/A&quot;&nbsp;</span></li>
                        </ol>
                    </li>
                </ol>
            </li>
            <li style="box-sizing: border-box; list-style: inherit;"><span style="box-sizing: border-box; background-color: rgb(255, 255, 255);">Check column AS, BA does not include &quot;Series, Class, Unit, Shares, Securities&quot;</span></li>
        </ol>
    </li>
</ol>
<p>
    &nbsp;</p>

