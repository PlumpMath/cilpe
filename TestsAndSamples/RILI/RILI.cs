using System;
using System.Collections;
using CILPE.Config;

namespace RILI
{
	class Product
	{
		public int color;
		public double price;

		public Product (int color, double price) 
		{
			this.color = color;
			this.price = price;
		}
	}

	abstract class Spec
	{
		[Inline]
		public Spec () {}

		[Inline]
		public abstract bool isSatisfiedBy (Product product);
	}

	class AndSpec : Spec
	{
		Spec x, y;

		[Inline]
		public AndSpec (Spec x, Spec y) {
			this.x = x;
			this.y = y;
		}

		[Inline]
		public override bool isSatisfiedBy (Product product)
		{
			return this.x.isSatisfiedBy(product) && this.y.isSatisfiedBy(product);
		}
	}

	class NotSpec : Spec
	{
		Spec x;

		[Inline]
		public NotSpec (Spec x) 
		{
			this.x = x;
		}

		[Inline]
		public override bool isSatisfiedBy (Product product)
		{
			return ! this.x.isSatisfiedBy(product);
		}
	}

	class ColorSpec : Spec
	{
		int color;

		[Inline]
		public ColorSpec (int color) 
		{
			this.color = color;
		}

		[Inline]
		public override bool isSatisfiedBy (Product product)
		{
			return product.color==this.color;
		}
	}

	class BelowPriceSpec : Spec
	{
		double price;

		[Inline]
		public BelowPriceSpec (double price) 
		{
			this.price = price;
		}

		[Inline]
		public override bool isSatisfiedBy (Product product)
		{
			return product.price<this.price;
		}
	}

	class ProductFinder
	{
		IEnumerable repository;

		public ProductFinder (IEnumerable repository)
		{
			this.repository = repository;
		}

		[Inline]
		public IList SelectBy (Spec spec)
		{
			IList foundProducts = new ArrayList();
   			IEnumerator products = repository.GetEnumerator();
			while (products.MoveNext()) 
			{
				Product product = (Product)products.Current;
				if (spec.isSatisfiedBy(product))
					foundProducts.Add(product);
			}
			return foundProducts;
		}

		[Specialize]
		public IList BelowPriceAvoidingAColor (double price, int color)
		{
			Spec spec = new AndSpec(new BelowPriceSpec(price), new NotSpec(new ColorSpec(color)));
			return SelectBy(spec);
		}
	}

	class RILI
	{
		static void Main(string[] args) {}
	}
}
