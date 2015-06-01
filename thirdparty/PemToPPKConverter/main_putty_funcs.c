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
#include "ssh.h"

enum { OSSH_DSA, OSSH_RSA };
int ber_read_id_len(void *source, int sourcelen, int *id, int *length, int *flags);

void saveKey(const struct Filename *filename, struct ssh2_userkey *key, char *passphrase)
{
    FILE *fp;
    unsigned char *pub_blob, *priv_blob, *priv_blob_encrypted;
    int pub_blob_len, priv_blob_len, priv_encrypted_len;
    int passlen;
    int cipherblk;
    int i;
    char *cipherstr;
    unsigned char priv_mac[20];

    /*
     * Fetch the key component blobs.
     */
	pub_blob = rsa2_public_blob(key->data, &pub_blob_len);
	priv_blob = rsa2_private_blob(key->data, &priv_blob_len);
    if (!pub_blob || !priv_blob) 
	{
		free(pub_blob);
		free(priv_blob);
		return 0;
    }

    /*
     * Determine encryption details, and encrypt the private blob.
     */
    if (passphrase) 
	{
		cipherstr = "aes256-cbc";
		cipherblk = 16;
    } 
	else 
	{
		cipherstr = "none";
		cipherblk = 1;
    }

    priv_encrypted_len = priv_blob_len + cipherblk - 1;
    priv_encrypted_len -= priv_encrypted_len % cipherblk;
    priv_blob_encrypted = safemalloc(priv_encrypted_len, sizeof(unsigned char));

    memset(priv_blob_encrypted, 0, priv_encrypted_len);
    memcpy(priv_blob_encrypted, priv_blob, priv_blob_len);

    /* Create padding based on the SHA hash of the unpadded blob. This prevents
     * too easy a known-plaintext attack on the last block. */
    SHA_Simple(priv_blob, priv_blob_len, priv_mac);
    memcpy(priv_blob_encrypted + priv_blob_len, priv_mac,
	   priv_encrypted_len - priv_blob_len);

    /* Now create the MAC. */
    {
		unsigned char *macdata;
		int maclen;
		unsigned char *p;
		int namelen = strlen("ssh-rsa");
		int enclen = strlen(cipherstr);
		int commlen = strlen(key->comment);
		SHA_State s;
		unsigned char mackey[20];
		char header[] = "putty-private-key-file-mac-key";

		maclen = (4 + namelen +
			  4 + enclen +
			  4 + commlen +
			  4 + pub_blob_len +
			  4 + priv_encrypted_len);
		macdata = safemalloc(maclen, sizeof(unsigned char));

		p = macdata;
#define DO_STR(s,len) PUT_32BIT(p,(len));memcpy(p+4,(s),(len));p+=4+(len)
		DO_STR("ssh-rsa", namelen);
		DO_STR(cipherstr, enclen);
		DO_STR(key->comment, commlen);
		DO_STR(pub_blob, pub_blob_len);
		DO_STR(priv_blob_encrypted, priv_encrypted_len);

		SHA_Init(&s);
		SHA_Bytes(&s, header, sizeof(header)-1);
		if (passphrase)
			SHA_Bytes(&s, passphrase, strlen(passphrase));

		SHA_Final(&s, mackey);
		hmac_sha1_simple(mackey, 20, macdata, maclen, priv_mac);
		memset(macdata, 0, maclen);
		free(macdata);
		memset(mackey, 0, sizeof(mackey));
		memset(&s, 0, sizeof(s));
    }

 //   if (passphrase) 
	//{
	//	unsigned char key[40];
	//	SHA_State s;

	//	passlen = strlen(passphrase);

	//	SHA_Init(&s);
	//	SHA_Bytes(&s, "\0\0\0\0", 4);
	//	SHA_Bytes(&s, passphrase, passlen);
	//	SHA_Final(&s, key + 0);
	//	SHA_Init(&s);
	//	SHA_Bytes(&s, "\0\0\0\1", 4);
	//	SHA_Bytes(&s, passphrase, passlen);
	//	SHA_Final(&s, key + 20);
	//	aes256_encrypt_pubkey(key, priv_blob_encrypted,
	//				  priv_encrypted_len);

	//	memset(key, 0, sizeof(key));
	//	memset(&s, 0, sizeof(s));
 //   }


    fp = f_open(*filename, "w", TRUE);
    if (!fp)
		return 0;

    fprintf(fp, "PuTTY-User-Key-File-2: %s\n", "ssh-rsa");
    fprintf(fp, "Encryption: %s\n", cipherstr);
    fprintf(fp, "Comment: %s\n", key->comment);
    fprintf(fp, "Public-Lines: %d\n", base64_lines(pub_blob_len));
    base64_encode(fp, pub_blob, pub_blob_len, 64);
    fprintf(fp, "Private-Lines: %d\n", base64_lines(priv_encrypted_len));
    base64_encode(fp, priv_blob_encrypted, priv_encrypted_len, 64);
    fprintf(fp, "Private-MAC: ");
    for (i = 0; i < 20; i++)
	fprintf(fp, "%02x", priv_mac[i]);
    fprintf(fp, "\n");
    fclose(fp);

    free(pub_blob);
    memset(priv_blob, 0, priv_blob_len);
    free(priv_blob);
    free(priv_blob_encrypted);
    return 1;
}


struct ssh2_userkey *openssh_read(const Filename *filename, char *passphrase, const char **errmsg_p)
{
    struct openssh_key *key = load_openssh_key(filename, errmsg_p);
    struct ssh2_userkey *retkey;
    unsigned char *p;
    int ret, id, len, flags;
    int i, num_integers;
    struct ssh2_userkey *retval = NULL;
    char *errmsg;
    unsigned char *blob;
    int blobsize = 0, blobptr, privptr;
    char *modptr = NULL;
    int modlen = 0;

    blob = NULL;

    if (!key)
		return NULL;

 //   if (key->encrypted) 
	//{
	///*
	// * Derive encryption key from passphrase and iv/salt:
	// * 
	// *  - let block A equal MD5(passphrase || iv)
	// *  - let block B equal MD5(A || passphrase || iv)
	// *  - block C would be MD5(B || passphrase || iv) and so on
	// *  - encryption key is the first N bytes of A || B
	// */
	//	struct MD5Context md5c;
	//	unsigned char keybuf[32];

	//	MD5Init(&md5c);
	//	MD5Update(&md5c, (unsigned char *)passphrase, strlen(passphrase));
	//	MD5Update(&md5c, (unsigned char *)key->iv, 8);
	//	MD5Final(keybuf, &md5c);

	//	MD5Init(&md5c);
	//	MD5Update(&md5c, keybuf, 16);
	//	MD5Update(&md5c, (unsigned char *)passphrase, strlen(passphrase));
	//	MD5Update(&md5c, (unsigned char *)key->iv, 8);
	//	MD5Final(keybuf+16, &md5c);

	//	/*
	//	 * Now decrypt the key blob.
	//	 */
	//	des3_decrypt_pubkey_ossh(keybuf, (unsigned char *)key->iv, key->keyblob, key->keyblob_len);

 //       memset(&md5c, 0, sizeof(md5c));
 //       memset(keybuf, 0, sizeof(keybuf));
	//}

    /*
     * Now we have a decrypted key blob, which contains an ASN.1
     * encoded private key. We must now untangle the ASN.1.
     *
     * We expect the whole key blob to be formatted as a SEQUENCE
     * (0x30 followed by a length code indicating that the rest of
     * the blob is part of the sequence). Within that SEQUENCE we
     * expect to see a bunch of INTEGERs. What those integers mean
     * depends on the key type:
     *
     *  - For RSA, we expect the integers to be 0, n, e, d, p, q,
     *    dmp1, dmq1, iqmp in that order. (The last three are d mod
     *    (p-1), d mod (q-1), inverse of q mod p respectively.)
     *
     *  - For DSA, we expect them to be 0, p, q, g, y, x in that
     *    order.
     */
    
    p = key->keyblob;

    /* Expect the SEQUENCE header. Take its absence as a failure to decrypt. */
    ret = ber_read_id_len(p, key->keyblob_len, &id, &len, &flags);
    p += ret;
    if (ret < 0 || id != 16) 
	{
		errmsg = "ASN.1 decoding failure";
		retval = NULL;
		goto error;
    }

    /* Expect a load of INTEGERs. */
    if (key->type == OSSH_RSA)
		num_integers = 9;
    else if (key->type == OSSH_DSA)
		num_integers = 6;
    else
		num_integers = 0;	       /* placate compiler warnings */

    /*
     * Space to create key blob in.
     */
    blobsize = 256+key->keyblob_len;
    blob = snewn(blobsize, unsigned char);
    PUT_32BIT(blob, 7);

    if (key->type == OSSH_DSA)
		memcpy(blob+4, "ssh-dss", 7);
    else if (key->type == OSSH_RSA)
		memcpy(blob+4, "ssh-rsa", 7);

    blobptr = 4+7;
    privptr = -1;

	for (i = 0; i < num_integers; i++) 
	{
		ret = ber_read_id_len(p, key->keyblob+key->keyblob_len-p,
					  &id, &len, &flags);
		p += ret;
		if (ret < 0 || id != 2 || key->keyblob + key->keyblob_len - p < len) 
		{
			errmsg = "ASN.1 decoding failure";
			retval = NULL;
			goto error;
		}

		if (i == 0) 
		{
			/*
			 * The first integer should be zero always (I think
			 * this is some sort of version indication).
			 */
			if (len != 1 || p[0] != 0) 
			{
				errmsg = "version number mismatch";
				goto error;
			}
		} 
		else if (key->type == OSSH_RSA) 
		{
			/*
			 * Integers 1 and 2 go into the public blob but in the
			 * opposite order; integers 3, 4, 5 and 8 go into the
			 * private blob. The other two (6 and 7) are ignored.
			 */
			if (i == 1) 
			{
			/* Save the details for after we deal with number 2. */
			modptr = (char *)p;
			modlen = len;
			} 
			else if (i != 6 && i != 7) 
			{
				PUT_32BIT(blob+blobptr, len);
				memcpy(blob+blobptr+4, p, len);
				blobptr += 4+len;

				if (i == 2) 
				{
					PUT_32BIT(blob+blobptr, modlen);
					memcpy(blob+blobptr+4, modptr, modlen);
					blobptr += 4+modlen;
					privptr = blobptr;
				}
			}
		} 
		else if (key->type == OSSH_DSA) 
		{
			/*
			 * Integers 1-4 go into the public blob; integer 5 goes
			 * into the private blob.
			 */
			PUT_32BIT(blob+blobptr, len);
			memcpy(blob+blobptr+4, p, len);
			blobptr += 4+len;

			if (i == 4)
				privptr = blobptr;
		}

		/* Skip past the number. */
		p += len;
	}

    /*
     * Now put together the actual key. Simplest way to do this is
     * to assemble our own key blobs and feed them to the createkey
     * functions; this is a bit faffy but it does mean we get all
     * the sanity checks for free.
     */
    retkey = snew(struct ssh2_userkey);
//    retkey->alg = &ssh_rsa;
//    retkey->data = retkey->alg->createkey(blob, privptr, blob+privptr, blobptr-privptr);
	retkey->data = rsa2_createkey(blob, privptr, blob+privptr, blobptr-privptr);
    if (!retkey->data) 
	{
		free(retkey);
		errmsg = "unable to create key data structure";
		goto error;
    }

    retkey->comment = dupstr("imported-openssh-key");
    errmsg = NULL;                     /* no error */
    retval = retkey;

    error:
    if (blob) 
	{
        memset(blob, 0, blobsize);
        free(blob);
    }
    memset(key->keyblob, 0, key->keyblob_size);
    free(key->keyblob);
    memset(key, 0, sizeof(*key));
    free(key);
    if (errmsg_p) 
		*errmsg_p = errmsg;

    return retval;
}


static int ber_read_id_len(void *source, int sourcelen, int *id, int *length, int *flags)
{
    unsigned char *p = (unsigned char *) source;

    if (sourcelen == 0)
		return -1;

    *flags = (*p & 0xE0);
    if ((*p & 0x1F) == 0x1F) 
	{
		*id = 0;
		while (*p & 0x80) 
		{
			p++, sourcelen--;
			if (sourcelen == 0)
				return -1;
			
			*id = (*id << 7) | (*p & 0x7F);
	}
	p++, sourcelen--;
    } else {
	*id = *p & 0x1F;
	p++, sourcelen--;
    }

    if (sourcelen == 0)
	return -1;

    if (*p & 0x80) {
	int n = *p & 0x7F;
	p++, sourcelen--;
	if (sourcelen < n)
	    return -1;
	*length = 0;
	while (n--)
	    *length = (*length << 8) | (*p++);
	sourcelen -= n;
    } else {
	*length = *p;
	p++, sourcelen--;
    }

    return p - (unsigned char *) source;
}
