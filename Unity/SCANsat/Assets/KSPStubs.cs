using System;

[AttributeUsage(AttributeTargets.Assembly)]
public class KSPAssembly : Attribute
{
	public string name;

	public int versionMajor;

	public int versionMinor;

	public int versionRevision;

	public KSPAssembly(string name, int versionMajor, int versionMinor)
	{
		this.name = name;
		this.versionMajor = versionMajor;
		this.versionMinor = versionMinor;
		versionRevision = 0;
	}

	public KSPAssembly(string name, int versionMajor, int versionMinor, int versionRevision)
	{
		this.name = name;
		this.versionMajor = versionMajor;
		this.versionMinor = versionMinor;
		this.versionRevision = versionRevision;
	}
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class KSPAssemblyDependency : Attribute
{
	public string name;

	public int versionMajor;

	public int versionMinor;

	public int versionRevision;

	public KSPAssemblyDependency(string name, int versionMajor, int versionMinor)
	{
		this.name = name;
		this.versionMajor = versionMajor;
		this.versionMinor = versionMinor;
		versionRevision = 0;
	}

	public KSPAssemblyDependency(string name, int versionMajor, int versionMinor, int versionRevision)
	{
		this.name = name;
		this.versionMajor = versionMajor;
		this.versionMinor = versionMinor;
		this.versionRevision = versionRevision;
	}
}
