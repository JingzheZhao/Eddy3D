import pandas as pd
import matplotlib.pyplot as plt
data = pd.read_csv("residuals.dat",skiprows=1, delimiter='\s+').iloc[:, 1:].shift(+1,axis=1).drop(["Time"], axis= 1)
plot = data.plot(logy= True, figsize=(15,5))
fig = plot.get_figure()
ax = plt.gca()
ax.legend(loc='upper right')
ax.set_xlabel("Iterations")
ax.set_ylabel("Residuals")
plt.savefig("residuals.png",dpi=72)