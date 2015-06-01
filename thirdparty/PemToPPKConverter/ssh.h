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

typedef unsigned int uint32;

#ifndef BIGNUM_INTERNAL
typedef void *Bignum;
#endif

extern Bignum Zero, One;

struct RSAKey 
{
    int bits;
    int bytes;

	Bignum modulus;
    Bignum exponent;
    Bignum private_exponent;
    Bignum p;
    Bignum q;
    Bignum iqmp;

	char *comment;
};


typedef struct {
    uint32 h[4];
} MD5_Core_State;

struct MD5Context 
{
    MD5_Core_State core;
    unsigned char block[64];
    int blkused;
    uint32 lenhi, lenlo;
};

void MD5Init(struct MD5Context *context);
void MD5Update(struct MD5Context *context, unsigned char const *buf, unsigned len);
void MD5Final(unsigned char digest[16], struct MD5Context *context);

Bignum bignum_from_bytes(const unsigned char *data, int nbytes);
Bignum bigmuladd(Bignum a, Bignum b, Bignum addend);
Bignum bigmul(Bignum a, Bignum b);
int bignum_cmp(Bignum a, Bignum b);
void freebn(Bignum b);
void decbn(Bignum bn);
Bignum copybn(Bignum orig);
int bignum_bitcount(Bignum bn);
int bignum_byte(Bignum bn, int i);