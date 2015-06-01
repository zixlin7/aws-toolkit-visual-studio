/*
 * PuTTY is copyright 1997-2007 Simon Tatham.

 * Portions copyright Robert de Bath, Joris van Rantwijk, Delian
 * Delchev, Andreas Schultz, Jeroen Massar, Wez Furlong, Nicolas Barry,
 * Justin Bradford, Ben Harris, Malcolm Smith, Ahmad Khalifa, Markus
 * Kuhn, and CORE SDI S.A.

 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation files
 * (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software,
 * and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:

 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT.  IN NO EVENT SHALL THE COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
 * CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include <stdio.h>
#include <string.h>

#include "puttyfuncs.h"

enum { OSSH_DSA, OSSH_RSA };



struct openssh_key* load_openssh_key(const Filename *filename, const char **errmsg_p)
{
    struct openssh_key *ret;
    FILE *fp;
    char *line = NULL;
    char *errmsg, *p;
    int headers_done;
    char base64_bit[4];
    int base64_chars = 0;

    ret = snew(struct openssh_key);
    ret->keyblob = NULL;
    ret->keyblob_len = ret->keyblob_size = 0;
    ret->encrypted = 0;
    memset(ret->iv, 0, sizeof(ret->iv));

    fp = f_open(*filename, "r", FALSE);
    if (!fp) {
		errmsg = "unable to open key file";
		goto error;
    }

    if (!(line = fgetline(fp))) 
	{
		errmsg = "unexpected end of file";
		goto error;
    }

    strip_crlf(line);
    if (0 != strncmp(line, "-----BEGIN ", 11) || 0 != strcmp(line+strlen(line)-16, "PRIVATE KEY-----")) 
	{
		errmsg = "file does not begin with OpenSSH key header";
		goto error;
    }
    if (!strcmp(line, "-----BEGIN RSA PRIVATE KEY-----"))
		ret->type = OSSH_RSA;
    else if (!strcmp(line, "-----BEGIN DSA PRIVATE KEY-----"))
		ret->type = OSSH_DSA;
    else 
	{
		errmsg = "unrecognised key type";
		goto error;
    }

    memset(line, 0, strlen(line));
    free(line);
    line = NULL;

    headers_done = 0;
    while (1) 
	{
		if (!(line = fgetline(fp))) 
		{
			errmsg = "unexpected end of file";
			goto error;
		}
		strip_crlf(line);
		if (0 == strncmp(line, "-----END ", 9) && 0 == strcmp(line+strlen(line)-16, "PRIVATE KEY-----"))
			break;		       /* done */

		if ((p = strchr(line, ':')) != NULL) 
		{
			if (headers_done) 
			{
				errmsg = "header found in body of key data";
				goto error;
			}

			*p++ = '\0';
			while (*p && isspace((unsigned char)*p)) 
				p++;

			if (!strcmp(line, "Proc-Type")) 
			{
				if (p[0] != '4' || p[1] != ',') 
				{
					errmsg = "Proc-Type is not 4 (only 4 is supported)";
					goto error;
				}
				p += 2;

				if (!strcmp(p, "ENCRYPTED"))
					ret->encrypted = 1;
			} 
			else if (!strcmp(line, "DEK-Info")) 
			{
				int i, j;

				if (strncmp(p, "DES-EDE3-CBC,", 13)) 
				{
					errmsg = "ciphers other than DES-EDE3-CBC not supported";
					goto error;
				}

				p += 13;
				for (i = 0; i < 8; i++) 
				{
					if (1 != sscanf(p, "%2x", &j))
						break;
					ret->iv[i] = j;
					p += 2;
				}

				if (i < 8) 
				{
					errmsg = "expected 16-digit iv in DEK-Info";
					goto error;
				}
			}
		}
		else 
		{
			headers_done = 1;

			p = line;
			while (isbase64(*p)) 
			{
				base64_bit[base64_chars++] = *p;
				if (base64_chars == 4) 
				{
                    unsigned char out[3];
                    int len;

                    base64_chars = 0;

                    len = base64_decode_atom(base64_bit, out);

                    if (len <= 0) 
					{
                        errmsg = "invalid base64 encoding";
                        goto error;
                    }

                    if (ret->keyblob_len + len > ret->keyblob_size) {
                        ret->keyblob_size = ret->keyblob_len + len + 256;
                        ret->keyblob = sresize(ret->keyblob, ret->keyblob_size, unsigned char);
                    }

                    memcpy(ret->keyblob + ret->keyblob_len, out, len);
                    ret->keyblob_len += len;

                    memset(out, 0, sizeof(out));
				}

				p++;
			}
		}
		memset(line, 0, strlen(line));
		free(line);
		line = NULL;
    }

    if (ret->keyblob_len == 0 || !ret->keyblob) 
	{
		errmsg = "key body not present";
		goto error;
    }

    if (ret->encrypted && ret->keyblob_len % 8 != 0) 
	{
		errmsg = "encrypted key blob is not a multiple of cipher block size";
		goto error;
    }

    memset(base64_bit, 0, sizeof(base64_bit));
    if (errmsg_p) 
		*errmsg_p = NULL;
    return ret;

    error:
		if (line) 
		{
			memset(line, 0, strlen(line));
			free(line);
			line = NULL;
		}
		memset(base64_bit, 0, sizeof(base64_bit));
		if (ret) 
		{
			if (ret->keyblob) 
			{
				memset(ret->keyblob, 0, ret->keyblob_size);
				free(ret->keyblob);
			}
			memset(ret, 0, sizeof(*ret));
			free(ret);
		}

		if (errmsg_p) 
			*errmsg_p = errmsg;
		return NULL;
}