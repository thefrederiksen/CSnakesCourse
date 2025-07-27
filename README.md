# Master CSnakes - C# and Python Interoperability

## A Comprehensive Journey into Seamless Cross-Language Development

![CSnakes Course](https://img.shields.io/badge/CSnakes-.NET%209.0-blue) ![Python](https://img.shields.io/badge/Python-3.9%2B-green) ![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

_Learn to harness the power of Python libraries directly within your .NET applications using the cutting-edge CSnakes runtime_

---

### Before You Begin

I'm available to help and support you throughout this course! Don't hesitate to reach out:

- **LinkedIn**: [Soren Frederiksen](https://www.linkedin.com/in/sorenfrederiksen/)
- **Course Support**: I'm committed to helping everyone succeed with CSnakes

---

## 🚀 Course Overview

This hands-on course takes you through **15 progressive projects** that demonstrate real-world C# and Python interoperability using CSnakes. From basic "Hello World" examples to production-ready trading applications with machine learning backends, you'll master the art of embedding Python seamlessly into .NET applications.

**What makes this course unique:**
- Real-world production patterns and best practices
- Progressive complexity from beginner to advanced concepts
- Complete trading application with ML pipeline
- Cross-platform development techniques
- Memory management and performance optimization

---

## 📋 Setup Instructions

Choose your platform for detailed setup instructions:

### Windows Setup (Recommended for Beginners)
📖 **[Complete Windows Setup Guide](./setup/setup_windows.md)**

### macOS Setup
📖 **[macOS Setup Guide](./setup/setup_macos.md)**

### Linux Setup  
📖 **[Linux Setup Guide](./setup/setup_linux.md)**

---

## 🛠 Prerequisites

- **.NET 9.0 SDK** or later
- **Python 3.9+** (supports up to Python 3.13)
- **Visual Studio 2022** (Community edition free) - *strongly recommended*
- **Basic C# knowledge** - Understanding of classes, methods, and basic OOP
- **Basic Python familiarity** - Helpful but not required

---

## 📚 Course Structure

### **Foundation Projects (1-4)**
Build your understanding of CSnakes fundamentals

| Project | Focus Area | Key Learning |
|---------|------------|--------------|
| **01. HelloWorld** | Basic Setup | First Python function call from C# |
| **02. Primitives & Return Types** | Data Types | Handling different data types across languages |
| **03. Collections & Tuples** | Complex Data | Working with lists, dictionaries, and tuples |
| **04. Managing Python** | Environment Management | Virtual environments and package management |

### **Intermediate Projects (5-10)**
Explore advanced integration patterns

| Project | Focus Area | Key Learning |
|---------|------------|--------------|
| **06. NumPy1** | Scientific Computing | NumPy arrays and buffer sharing |
| **07. Numpy2** | Performance | Memory-efficient array operations |
| **08. CSnakes Exceptions** | Error Management | Robust error handling across languages |
| **09. DataTable Sample** | Data Processing | Working with structured data |
| **10. KMeans In Process** | Machine Learning | In-process ML with scikit-learn |

### **Advanced Projects (11-15)**
Master production-ready applications

| Project | Focus Area | Key Learning |
|---------|------------|--------------|
| **12. Format Markdown** | Text Processing | Document generation and formatting |
| **13. Generators Sample** | Memory Efficiency | Python generators in C# applications |
| **14. Progress From Python** | Progress Reporting | Async progress updates from Python |
| **15. Async Python** | Concurrency | Asynchronous Python operations |
| **BlazorTrader** | Full Application | Complete trading system with ML backend |

### **Specialized Applications**

- **TalkToMyCode**: WinForms application for AI-powered code analysis
- **TestPython**: Testing patterns and Python integration verification

---

## 🎯 Learning Objectives

By completing this course, you will:

- ✅ **Master CSnakes Runtime** - Embed Python seamlessly in .NET applications
- ✅ **Handle Data Exchange** - Efficiently pass data between C# and Python
- ✅ **Manage Python Environments** - Work with virtual environments and packages
- ✅ **Optimize Performance** - Implement memory-efficient buffer sharing
- ✅ **Build Production Apps** - Create real-world applications with ML integration
- ✅ **Handle Errors Gracefully** - Implement robust cross-language error handling
- ✅ **Deploy Successfully** - Understand deployment patterns and considerations

---

## 🏆 Capstone Project: BlazorTrader

The course culminates in **BlazorTrader** - a sophisticated trading application showcasing production-ready patterns:

### **Architecture Overview**
- **Frontend**: Blazor Server with real-time trading UI
- **Backend**: C# orchestrating Python ML pipeline
- **ML Pipeline**: Multi-stage workflow with XGBoost models
- **Data**: S&P 500 stock analysis with technical indicators

### **ML Pipeline Stages**
1. **Data Download** - Fetch S&P 500 stock data via yfinance
2. **Technical Indicators** - Calculate RSI, MACD, Bollinger Bands
3. **Training Data Creation** - Feature engineering for ML models
4. **Model Training** - XGBoost binary classification
5. **Prediction & Analysis** - Generate trading signals
6. **Explainability** - AI-powered results interpretation

---

## 🚦 Getting Started

1. **Choose your setup guide** based on your operating system
2. **Start with HelloWorld** to verify your environment
3. **Progress sequentially** through the numbered projects
4. **Run each project** to see concepts in action
5. **Experiment and modify** - the best way to learn!

### Running Your First Project

```bash
# Open the solution in Visual Studio
# Right-click "01. HelloWorld" project
# Select "Set as Startup Project"  
# Press F5 or click Start

# Expected output:
# Hello from Python: World
```

---

## 🔧 Important Notes for Specific Projects

### **BlazorTrader Considerations**
- **Python Dependencies**: Large ML stack (XGBoost, pandas, yfinance)
- **Data Requirements**: ~500MB for S&P 500 historical data  
- **First Run**: May take 10-15 minutes for initial data download
- **API Considerations**: Uses free yfinance API (no key required)

### **Memory Management Projects**
- **NumPy Projects**: Demonstrate zero-copy buffer sharing
- **Large Datasets**: Sample projects optimized for memory efficiency
- **Performance Monitoring**: Built-in timing and memory usage examples

### **Cross-Platform Notes**
- **Python Path Detection**: Automatic Python discovery on all platforms
- **Virtual Environments**: Full support for venv and conda
- **Package Management**: requirements.txt handled automatically

---

## 📖 Essential Resources

### **In This Repository**
- **[CLAUDE.md](./CLAUDE.md)** - Detailed architecture and development notes
- **Individual Project READMEs** - Specific guidance for each lesson
- **[Course Solution File](./CSnakes%20Course.sln)** - Visual Studio solution

### **External Resources**
- **[CSnakes Documentation](https://tonybaloney.github.io/CSnakes/)**
- **[CSnakes GitHub Repository](https://github.com/tonybaloney/CSnakes)**
- **[.NET 9.0 Documentation](https://docs.microsoft.com/dotnet/)**

---

## 💰 Cost Considerations

This course is designed to be **completely free** to run:

- **No API Keys Required** - Uses free data sources (yfinance)
- **No Cloud Services** - Everything runs locally
- **Optional Enhancements** - All paid services are clearly marked as optional
- **Resource Monitoring** - Built-in guidance on system resource usage

---

## 🐍 Python Integration Highlights

### **Supported Python Versions**
- Python 3.9 through 3.13
- Automatic Python discovery and setup
- Full CPython compatibility (NumPy, pandas, scikit-learn, etc.)

### **Key Libraries Demonstrated** 
- **NumPy** - High-performance arrays and mathematical operations
- **Pandas** - Data manipulation and analysis
- **XGBoost** - Gradient boosting machine learning
- **yfinance** - Financial data acquisition
- **ta** - Technical analysis indicators

### **Advanced Features**
- **Zero-copy buffer sharing** with NumPy arrays
- **Async Python operations** from C#
- **Progress reporting** from Python to C#
- **Exception propagation** across language boundaries

---

## 🎉 ABOVE ALL ELSE

**Have fun with this course!** You couldn't have picked a better time to learn CSnakes. The intersection of C# and Python opens incredible possibilities - from leveraging Python's rich ML ecosystem in enterprise .NET applications to rapid prototyping with production deployment paths.

This technology is revolutionizing how we think about cross-language development, and you're getting in at the perfect time!

---

## 🚀 Production Deployment Guidelines

### Python Installation for CSnakes-Based Projects

When deploying or building a project that uses CSnakes (such as FaceVault), it's important to ensure a consistent development and build environment. CSnakes relies on a locally installed Python runtime to analyze and generate bindings between Python and C# at build time.

#### 🧱 Recommended Deployment Practice

To avoid discrepancies across developer machines or build servers, we recommend the following:

**🔁 Install the Same Python Version Everywhere**
- Choose a stable Python version, such as 3.10.x
- Install this version on:
  - All developer workstations
  - All build server environments

This avoids unexpected behavior due to differences in the way Python interprets annotations, dependencies, or library versions.

**📦 Tools You Can Use**
- pyenv (Linux/macOS)
- pyenv-win (Windows)
- Direct installer from python.org

**📁 Project Structure Notes**
- Make sure your .py files with CSnakes bindings are part of the project and available at build time
- Keep dependencies version-controlled (e.g., requirements.txt or a lockfile)

**✅ Summary**
While CSnakes lets you embed Python for runtime, you still need Python installed to build unless you pre-generate bindings. Installing a consistent Python version across all environments keeps builds stable and avoids platform-specific issues.

---

## 📄 License

This course is released under the [MIT License](./LICENSE).

---

## 🤝 Contributing

Found an issue or have suggestions? I welcome contributions!

1. Open an issue describing the problem or enhancement
2. Fork the repository and make your changes  
3. Submit a pull request with clear descriptions
4. **Reach out directly** - I'm always happy to discuss improvements

---

## 📞 Support & Community

Remember, I'm here to help! This course represents hundreds of hours of development and testing across different platforms and scenarios. If you get stuck, have questions, or want to share what you've built:

**Connect with me on [LinkedIn](https://www.linkedin.com/in/sorenfrederiksen/)** - I personally respond to all course-related questions and love seeing what people build with CSnakes!

---

*Happy coding with CSnakes! 🐍⚡*