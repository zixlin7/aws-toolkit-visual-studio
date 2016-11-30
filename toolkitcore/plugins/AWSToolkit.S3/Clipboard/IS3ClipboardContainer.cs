using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.S3.Clipboard
{
    public interface IS3ClipboardContainer
    {
        S3Clipboard Clipboard { get; set; }
    }
}
