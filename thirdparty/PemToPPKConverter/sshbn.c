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
#include <assert.h>
#include <stdlib.h>
#include <string.h>

typedef unsigned __int32 BignumInt;
typedef unsigned __int64 BignumDblInt;
#define BIGNUM_INT_MASK  0xFFFFFFFFUL
#define BIGNUM_TOP_BIT   0x80000000UL
#define BIGNUM_INT_BITS  32

#define BIGNUM_INT_BYTES (BIGNUM_INT_BITS / 8)

#define BIGNUM_INTERNAL
typedef BignumInt *Bignum;

#define MUL_WORD(w1, w2) ((BignumDblInt)w1 * w2)
/* Note: MASM interprets array subscripts in the macro arguments as
 * assembler syntax, which gives the wrong answer. Don't supply them.
 * <http://msdn2.microsoft.com/en-us/library/bf1dw62z.aspx> */
#define DIVMOD_WORD(q, r, hi, lo, w) do { \
    __asm mov edx, hi \
    __asm mov eax, lo \
    __asm div w \
    __asm mov r, edx \
    __asm mov q, eax \
} while(0)

#include "ssh.h"
#include "puttyfuncs.h"

void internal_mul(BignumInt *a, BignumInt *b, BignumInt *c, int len);
void internal_mod(BignumInt *a, int alen, BignumInt *m, int mlen, BignumInt *quot, int qshift);
void internal_add_shifted(BignumInt *number, unsigned n, int shift);


BignumInt bnZero[1] = { 0 };
BignumInt bnOne[2] = { 1, 1 };

/*
 * The Bignum format is an array of `BignumInt'. The first
 * element of the array counts the remaining elements. The
 * remaining elements express the actual number, base 2^BIGNUM_INT_BITS, _least_
 * significant digit first. (So it's trivial to extract the bit
 * with value 2^n for any n.)
 *
 * All Bignums in this module are positive. Negative numbers must
 * be dealt with outside it.
 *
 * INVARIANT: the most significant word of any Bignum must be
 * nonzero.
 */

Bignum Zero = bnZero, One = bnOne;

static Bignum newbn(int length)
{
    Bignum b = snewn(length + 1, BignumInt);
    if (!b)
	{
		abort();		       /* FIXME */
	}

    memset(b, 0, (length + 1) * sizeof(*b));
    b[0] = length;
    return b;
}


Bignum bignum_from_bytes(const unsigned char *data, int nbytes)
{
    Bignum result;
    int w, i;

    w = (nbytes + BIGNUM_INT_BYTES - 1) / BIGNUM_INT_BYTES; /* bytes->words */

    result = newbn(w);
    for (i = 1; i <= w; i++)
	{
		result[i] = 0;
	}

    for (i = nbytes; i--;) 
	{
		unsigned char byte = *data++;
		result[1 + i / BIGNUM_INT_BYTES] |= byte << (8*i % BIGNUM_INT_BITS);
    }

    while (result[0] > 1 && result[result[0]] == 0)
		result[0]--;

    return result;
}


/*
 * Non-modular multiplication.
 */
Bignum bigmul(Bignum a, Bignum b)
{
    return bigmuladd(a, b, NULL);
}

/*
 * Non-modular multiplication and addition.
 */
Bignum bigmuladd(Bignum a, Bignum b, Bignum addend)
{
    int alen = a[0], blen = b[0];
    int mlen = (alen > blen ? alen : blen);
    int rlen, i, maxspot;
    BignumInt *workspace;
    Bignum ret;

    /* mlen space for a, mlen space for b, 2*mlen for result */
    workspace = snewn(mlen * 4, BignumInt);
    for (i = 0; i < mlen; i++) 
	{
		workspace[0 * mlen + i] = (mlen - i <= (int)a[0] ? a[mlen - i] : 0);
		workspace[1 * mlen + i] = (mlen - i <= (int)b[0] ? b[mlen - i] : 0);
    }

    internal_mul(workspace + 0 * mlen, workspace + 1 * mlen, workspace + 2 * mlen, mlen);

    /* now just copy the result back */
    rlen = alen + blen + 1;
    if (addend && rlen <= (int)addend[0])
		rlen = addend[0] + 1;

    ret = newbn(rlen);
    maxspot = 0;
    for (i = 1; i <= (int)ret[0]; i++) 
	{
		ret[i] = (i <= 2 * mlen ? workspace[4 * mlen - i] : 0);
		if (ret[i] != 0)
			maxspot = i;
    }
    ret[0] = maxspot;

    /* now add in the addend, if any */
    if (addend) 
	{
		BignumDblInt carry = 0;
		for (i = 1; i <= rlen; i++) 
		{
			carry += (i <= (int)ret[0] ? ret[i] : 0);
			carry += (i <= (int)addend[0] ? addend[i] : 0);
			ret[i] = (BignumInt) carry & BIGNUM_INT_MASK;
			carry >>= BIGNUM_INT_BITS;
			if (ret[i] != 0 && i > maxspot)
				maxspot = i;
		}
    }

    ret[0] = maxspot;

    free(workspace);
    return ret;
}

/*
 * Return a byte from a bignum; 0 is least significant, etc.
 */
int bignum_byte(Bignum bn, int i)
{
    if (i >= (int)(BIGNUM_INT_BYTES * bn[0]))
	return 0;		       /* beyond the end */
    else
	return (bn[i / BIGNUM_INT_BYTES + 1] >>
		((i % BIGNUM_INT_BYTES)*8)) & 0xFF;
}

/*
 * Return the bit count of a bignum, for SSH-1 encoding.
 */
int bignum_bitcount(Bignum bn)
{
    int bitcount = bn[0] * BIGNUM_INT_BITS - 1;
    while (bitcount >= 0 && (bn[bitcount / BIGNUM_INT_BITS + 1] >> (bitcount % BIGNUM_INT_BITS)) == 0) 
		bitcount--;

    return bitcount + 1;
}

/*
 * Compare two bignums. Returns like strcmp.
 */
int bignum_cmp(Bignum a, Bignum b)
{
    int amax = a[0], bmax = b[0];
    int i = (amax > bmax ? amax : bmax);
    while (i) 
	{
		BignumInt aval = (i > amax ? 0 : a[i]);
		BignumInt bval = (i > bmax ? 0 : b[i]);
		if (aval < bval)
			return -1;
		if (aval > bval)
			return +1;
		i--;
    }

    return 0;
}

void freebn(Bignum b)
{
    /*
     * Burn the evidence, just in case.
     */
    memset(b, 0, sizeof(b[0]) * (b[0] + 1));
    free(b);
}

Bignum copybn(Bignum orig)
{
    Bignum b = snewn(orig[0] + 1, BignumInt);
    if (!b)
		abort();		       /* FIXME */

    memcpy(b, orig, (orig[0] + 1) * sizeof(*b));
    return b;
}

/*
 * Decrement a number.
 */
void decbn(Bignum bn)
{
    int i = 1;
    while (i < (int)bn[0] && bn[i] == 0)
		bn[i++] = BIGNUM_INT_MASK;

    bn[i]--;
}

/*
 * Compute (p * q) % mod.
 * The most significant word of mod MUST be non-zero.
 * We assume that the result array is the same size as the mod array.
 */
Bignum modmul(Bignum p, Bignum q, Bignum mod)
{
    BignumInt *a, *n, *m, *o;
    int mshift;
    int pqlen, mlen, rlen, i, j;
    Bignum result;

    /* Allocate m of size mlen, copy mod to m */
    /* We use big endian internally */
    mlen = mod[0];
    m = snewn(mlen, BignumInt);
    for (j = 0; j < mlen; j++)
	{
		m[j] = mod[mod[0] - j];
	}

    /* Shift m left to make msb bit set */
    for (mshift = 0; mshift < BIGNUM_INT_BITS-1; mshift++)
		if ((m[0] << mshift) & BIGNUM_TOP_BIT)
			break;

	if (mshift) 
	{
		for (i = 0; i < mlen - 1; i++)
			m[i] = (m[i] << mshift) | (m[i + 1] >> (BIGNUM_INT_BITS - mshift));

		m[mlen - 1] = m[mlen - 1] << mshift;
	}

	pqlen = (p[0] > q[0] ? p[0] : q[0]);

	/* Allocate n of size pqlen, copy p to n */
	n = snewn(pqlen, BignumInt);
	i = pqlen - p[0];
	for (j = 0; j < i; j++)
		n[j] = 0;
	for (j = 0; j < (int)p[0]; j++)
		n[i + j] = p[p[0] - j];

	/* Allocate o of size pqlen, copy q to o */
	o = snewn(pqlen, BignumInt);
	i = pqlen - q[0];
	for (j = 0; j < i; j++)
		o[j] = 0;
	for (j = 0; j < (int)q[0]; j++)
		o[i + j] = q[q[0] - j];

	/* Allocate a of size 2*pqlen for result */
	a = snewn(2 * pqlen, BignumInt);

	/* Main computation */
	internal_mul(n, o, a, pqlen);
	internal_mod(a, pqlen * 2, m, mlen, NULL, 0);

	/* Fixup result in case the modulus was shifted */
	if (mshift) 
	{
		for (i = 2 * pqlen - mlen - 1; i < 2 * pqlen - 1; i++)
			a[i] = (a[i] << mshift) | (a[i + 1] >> (BIGNUM_INT_BITS - mshift));
		
		a[2 * pqlen - 1] = a[2 * pqlen - 1] << mshift;
		internal_mod(a, pqlen * 2, m, mlen, NULL, 0);

		for (i = 2 * pqlen - 1; i >= 2 * pqlen - mlen; i--)
			a[i] = (a[i] >> mshift) | (a[i - 1] << (BIGNUM_INT_BITS - mshift));
	}

	/* Copy result to buffer */
	rlen = (mlen < pqlen * 2 ? mlen : pqlen * 2);
	result = newbn(rlen);
	for (i = 0; i < rlen; i++)
		result[result[0] - i] = a[i + 2 * pqlen - rlen];
	
	while (result[0] > 1 && result[result[0]] == 0)
		result[0]--;

	/* Free temporary arrays */
	for (i = 0; i < 2 * pqlen; i++)
		a[i] = 0;

	free(a);
	for (i = 0; i < mlen; i++)
		m[i] = 0;
	
	free(m);
	for (i = 0; i < pqlen; i++)
		n[i] = 0;
	
	free(n);
	for (i = 0; i < pqlen; i++)
		o[i] = 0;
	
	free(o);

    return result;
}

/*
 * Compute c = a * b.
 * Input is in the first len words of a and b.
 * Result is returned in the first 2*len words of c.
 */
static void internal_mul(BignumInt *a, BignumInt *b,
			 BignumInt *c, int len)
{
    int i, j;
    BignumDblInt t;

    for (j = 0; j < 2 * len; j++)
		c[j] = 0;

    for (i = len - 1; i >= 0; i--) 
	{
		t = 0;
		for (j = len - 1; j >= 0; j--) 
		{
			t += MUL_WORD(a[i], (BignumDblInt) b[j]);
			t += (BignumDblInt) c[i + j + 1];
			c[i + j + 1] = (BignumInt) t;
			t = t >> BIGNUM_INT_BITS;
		}
		c[i] = (BignumInt) t;
    }
}

/*
 * Compute a = a % m.
 * Input in first alen words of a and first mlen words of m.
 * Output in first alen words of a
 * (of which first alen-mlen words will be zero).
 * The MSW of m MUST have its high bit set.
 * Quotient is accumulated in the `quotient' array, which is a Bignum
 * rather than the internal bigendian format. Quotient parts are shifted
 * left by `qshift' before adding into quot.
 */
static void internal_mod(BignumInt *a, int alen,
			 BignumInt *m, int mlen,
			 BignumInt *quot, int qshift)
{
    BignumInt m0, m1;
    unsigned int h;
    int i, k;

    m0 = m[0];
    if (mlen > 1)
		m1 = m[1];
    else
		m1 = 0;

    for (i = 0; i <= alen - mlen; i++) 
	{
		BignumDblInt t;
		unsigned int q, r, c, ai1;

		if (i == 0) 
		{
			h = 0;
		} 
		else 
		{
			h = a[i - 1];
			a[i - 1] = 0;
		}

		if (i == alen - 1)
			ai1 = 0;
		else
			ai1 = a[i + 1];

		/* Find q = h:a[i] / m0 */
		if (h >= m0) 
		{
			/*
			 * Special case.
			 * 
			 * To illustrate it, suppose a BignumInt is 8 bits, and
			 * we are dividing (say) A1:23:45:67 by A1:B2:C3. Then
			 * our initial division will be 0xA123 / 0xA1, which
			 * will give a quotient of 0x100 and a divide overflow.
			 * However, the invariants in this division algorithm
			 * are not violated, since the full number A1:23:... is
			 * _less_ than the quotient prefix A1:B2:... and so the
			 * following correction loop would have sorted it out.
			 * 
			 * In this situation we set q to be the largest
			 * quotient we _can_ stomach (0xFF, of course).
			 */
			q = BIGNUM_INT_MASK;
		} 
		else 
		{
			/* Macro doesn't want an array subscript expression passed
			 * into it (see definition), so use a temporary. */
			BignumInt tmplo = a[i];
			DIVMOD_WORD(q, r, h, tmplo, m0);

			/* Refine our estimate of q by looking at
			 h:a[i]:a[i+1] / m0:m1 */
			t = MUL_WORD(m1, q);
			if (t > ((BignumDblInt) r << BIGNUM_INT_BITS) + ai1) 
			{
				q--;
				t -= m1;
				r = (r + m0) & BIGNUM_INT_MASK;     /* overflow? */
				if (r >= (BignumDblInt) m0 && t > ((BignumDblInt) r << BIGNUM_INT_BITS) + ai1) 
					q--;
			}
		}

		/* Subtract q * m from a[i...] */
		c = 0;
		for (k = mlen - 1; k >= 0; k--) 
		{
			t = MUL_WORD(q, m[k]);
			t += c;
			c = (unsigned)(t >> BIGNUM_INT_BITS);
			if ((BignumInt) t > a[i + k])
			c++;
			a[i + k] -= (BignumInt) t;
		}

		/* Add back m in case of borrow */
		if (c != h) 
		{
			t = 0;
			for (k = mlen - 1; k >= 0; k--) 
			{
				t += m[k];
				t += a[i + k];
				a[i + k] = (BignumInt) t;
				t = t >> BIGNUM_INT_BITS;
			}
			q--;
		}

		if (quot)
			internal_add_shifted(quot, q, qshift + BIGNUM_INT_BITS * (alen - mlen - i));
    }
}

static void internal_add_shifted(BignumInt *number, unsigned n, int shift)
{
    int word = 1 + (shift / BIGNUM_INT_BITS);
    int bshift = shift % BIGNUM_INT_BITS;
    BignumDblInt addend;

    addend = (BignumDblInt)n << bshift;

    while (addend) 
	{
		addend += number[word];
		number[word] = (BignumInt) addend & BIGNUM_INT_MASK;
		addend >>= BIGNUM_INT_BITS;
		word++;
    }
}