import torch
import torch.nn as nn
import torch.optim as optim
import numpy as np

# Dane 0..99, etykieta: 1 jeśli > 50, inaczej 0
X = torch.tensor(np.arange(100).reshape(-1, 1), dtype=torch.float32)
y = (X > 50).float()

# Bardzo prosty model: 1 -> 1
model = nn.Sequential(
    nn.Linear(1, 1),
    nn.Sigmoid()
)

loss_fn = nn.BCELoss()
optimizer = optim.Adam(model.parameters(), lr=0.1)

print("Trening...")
for epoch in range(500):
    pred = model(X)
    loss = loss_fn(pred, y)
    optimizer.zero_grad()
    loss.backward()
    optimizer.step()
    if epoch % 100 == 0:
        acc = (pred.round() == y).float().mean() * 100
        print(f"Epoch {epoch}, Loss: {loss.item():.4f}, Acc: {acc:.1f}%")

# Test
model.eval()
test_x = torch.tensor([[5.0], [60.0], [20.0], [80.0]])
raw_pred = model(test_x)
print("\nFINALNE testy:")
print("test_x:", test_x.flatten())
print("raw pred:", [f"{p:.4f}" for p in raw_pred.squeeze().detach().numpy()])
print("round pred:", raw_pred.round())
print("Pełna dokładność:", ((model(X).round() == y).float().mean() * 100).item(), "%")
