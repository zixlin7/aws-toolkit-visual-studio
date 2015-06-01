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
#include <stdlib.h>
#include <string.h>

#include "puttyfuncs.h"
#include "ssh.h"

void *rsa2_newkey(char *data, int len);
void getstring(char **data, int *datalen, char **p, int *length);
Bignum getmp(char **data, int *datalen);
int rsa_verify(struct RSAKey *key);
void rsa2_freekey(void *key);

void *rsa2_createkey(unsigned char *pub_blob, int pub_len,
			    unsigned char *priv_blob, int priv_len)
{
    struct RSAKey *rsa;
    char *pb = (char *) priv_blob;

	rsa = rsa2_newkey((char *) pub_blob, pub_len);
    rsa->private_exponent = getmp(&pb, &priv_len);
	rsa->p = getmp(&pb, &priv_len);
    rsa->q = getmp(&pb, &priv_len);
    rsa->iqmp = getmp(&pb, &priv_len);


    if (!rsa_verify(rsa)) 
	{
		rsa2_freekey(rsa);
		return NULL;
    }

    return rsa;
}

static void *rsa2_newkey(char *data, int len)
{
    char *p;
    int slen;
    struct RSAKey *rsa;

    rsa = snew(struct RSAKey);

    if (!rsa)
		return NULL;
    getstring(&data, &len, &p, &slen);

    if (!p || slen != 7 || memcmp(p, "ssh-rsa", 7)) 
	{
		free(rsa);
		return NULL;
    }

    rsa->exponent = getmp(&data, &len);
    rsa->modulus = getmp(&data, &len);
    rsa->private_exponent = NULL;
    rsa->comment = NULL;

    return rsa;
}

void freersakey(struct RSAKey *key)
{
    if (key->modulus)
		freebn(key->modulus);
    if (key->exponent)
		freebn(key->exponent);
    if (key->private_exponent)
		freebn(key->private_exponent);
    if (key->comment)
		free(key->comment);
}

static void rsa2_freekey(void *key)
{
    struct RSAKey *rsa = (struct RSAKey *) key;
    freersakey(rsa);
    free(rsa);
}

/*
 * Verify that the public data in an RSA key matches the private
 * data. We also check the private data itself: we ensure that p >
 * q and that iqmp really is the inverse of q mod p.
 */
int rsa_verify(struct RSAKey *key)
{
    Bignum n, ed, pm1, qm1;
    int cmp;

    /* n must equal pq. */
    n = bigmul(key->p, key->q);

	cmp = bignum_cmp(n, key->modulus);
	freebn(n);

    if (cmp != 0)
		return 0;

	/* e * d must be congruent to 1, modulo (p-1) and modulo (q-1). */
	pm1 = copybn(key->p);
	decbn(pm1);
	ed = modmul(key->exponent, key->private_exponent, pm1);
	cmp = bignum_cmp(ed, One);
	free(ed);
    
	if (cmp != 0)
		return 0;

    qm1 = copybn(key->q);
    decbn(qm1);
    ed = modmul(key->exponent, key->private_exponent, qm1);
    cmp = bignum_cmp(ed, One);
    free(ed);

    if (cmp != 0)
		return 0;

    /*
     * Ensure p > q.
     */
    if (bignum_cmp(key->p, key->q) <= 0)
		return 0;

    /*
     * Ensure iqmp * q is congruent to 1, modulo p.
     */
    n = modmul(key->iqmp, key->q, key->p);
    cmp = bignum_cmp(n, One);
    free(n);

    if (cmp != 0)
		return 0;

	return 1;
}


unsigned char *rsa2_public_blob(void *key, int *len)
{
    struct RSAKey *rsa = (struct RSAKey *) key;
    int elen, mlen, bloblen;
    int i;
    unsigned char *blob, *p;

    elen = (bignum_bitcount(rsa->exponent) + 8) / 8;
    mlen = (bignum_bitcount(rsa->modulus) + 8) / 8;

    /*
     * string "ssh-rsa", mpint exp, mpint mod. Total 19+elen+mlen.
     * (three length fields, 12+7=19).
     */
    bloblen = 19 + elen + mlen;
    blob = snewn(bloblen, unsigned char);
    p = blob;
    PUT_32BIT(p, 7);
    p += 4;
    memcpy(p, "ssh-rsa", 7);
    p += 7;
    PUT_32BIT(p, elen);
    p += 4;

    for (i = elen; i--;)
		*p++ = bignum_byte(rsa->exponent, i);

    PUT_32BIT(p, mlen);
    p += 4;
    
	for (i = mlen; i--;)
		*p++ = bignum_byte(rsa->modulus, i);

	*len = bloblen;
    return blob;
}

unsigned char *rsa2_private_blob(void *key, int *len)
{
    struct RSAKey *rsa = (struct RSAKey *) key;
    int dlen, plen, qlen, ulen, bloblen;
    int i;
    unsigned char *blob, *p;

    dlen = (bignum_bitcount(rsa->private_exponent) + 8) / 8;
    plen = (bignum_bitcount(rsa->p) + 8) / 8;
    qlen = (bignum_bitcount(rsa->q) + 8) / 8;
    ulen = (bignum_bitcount(rsa->iqmp) + 8) / 8;

    /*
     * mpint private_exp, mpint p, mpint q, mpint iqmp. Total 16 +
     * sum of lengths.
     */
    bloblen = 16 + dlen + plen + qlen + ulen;
    blob = snewn(bloblen, unsigned char);
    p = blob;
    PUT_32BIT(p, dlen);
    p += 4;

    for (i = dlen; i--;)
		*p++ = bignum_byte(rsa->private_exponent, i);

    PUT_32BIT(p, plen);
    p += 4;
    for (i = plen; i--;)
		*p++ = bignum_byte(rsa->p, i);

    PUT_32BIT(p, qlen);
    p += 4;
    
	for (i = qlen; i--;)
		*p++ = bignum_byte(rsa->q, i);

    PUT_32BIT(p, ulen);
    p += 4;
    for (i = ulen; i--;)
		*p++ = bignum_byte(rsa->iqmp, i);


	*len = bloblen;
    return blob;
}


static void getstring(char **data, int *datalen, char **p, int *length)
{
    *p = NULL;
    if (*datalen < 4)
		return;

    *length = GET_32BIT(*data);
    *datalen -= 4;
    *data += 4;

    if (*datalen < *length)
		return;
    *p = *data;
    *data += *length;
    *datalen -= *length;
}


Bignum getmp(char **data, int *datalen)
{
    char *p;
    int length;
    Bignum b;

    getstring(data, datalen, &p, &length);
    if (!p)
		return NULL;

    b = bignum_from_bytes((unsigned char *)p, length);
    return b;
}