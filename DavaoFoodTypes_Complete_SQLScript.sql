-- Davao City Food Types Database Update Script with Complete Nutritional Properties
-- Execute this in SQL Server Management Studio (SSMS)
-- This script includes all evidence-based algorithm nutritional fields

-- First, clear existing food preferences to avoid foreign key constraints
DELETE FROM FoodPreferences;

-- Clear existing food types 
DELETE FROM FoodTypes;

-- Reset identity seed
DBCC CHECKIDENT('FoodTypes', RESEED, 0);

-- Insert comprehensive food types with complete nutritional properties
INSERT INTO FoodTypes (Name, Description, CuisineType, IsHealthy, IsLowCalorie, IsHighProtein, IsNutrientDense, IsVitaminRich, IsLowGlycemic, IsLowSodium, IsHeartHealthy, IsIronRich, ContainsGluten, ContainsDairy, ContainsNuts, ContainsMeat, IsVegetarian, IsVegan) VALUES

-- Filipino/Local Davao Specialties
('Durian', 'Davao''s famous king of fruits with strong aroma', 'Filipino', 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Durian Ice Cream', 'Creamy dessert made from Davao durian', 'Filipino', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Durian Candy', 'Sweet candy made from local durian', 'Filipino', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Pomelo', 'Large citrus fruit native to Davao', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Marang', 'Local tropical fruit similar to jackfruit', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Lanzones', 'Small sweet tropical fruit clusters', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Rambutan', 'Hairy red tropical fruit', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Mangosteen', 'Purple tropical fruit with white segments', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Davao Longganisa', 'Local sweet pork sausage', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Tuna Belly', 'Fresh grilled tuna belly, Davao specialty', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Grilled Tuna Jaw', 'Large grilled tuna jaw with tender meat', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Tuna Sashimi', 'Fresh raw tuna slices', 'Japanese', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Tuna Kinilaw', 'Raw tuna cured in vinegar and spices', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Inihaw na Bangus', 'Grilled milkfish stuffed with tomatoes and onions', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Lechon Belly', 'Roasted pork belly with crispy skin', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Lechon Kawali', 'Deep-fried crispy pork belly', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Crispy Pata', 'Deep-fried pork leg with crispy skin', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Adobo', 'Filipino braised pork or chicken in soy sauce and vinegar', 'Filipino', 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Sinigang', 'Sour soup with tamarind and vegetables', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Kare-Kare', 'Oxtail stew in peanut sauce', 'Filipino', 1, 0, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 0, 0),
('Bulalo', 'Beef shank soup with bone marrow', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Tinola', 'Chicken soup with ginger and green papaya', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Pancit Canton', 'Stir-fried wheat noodles with vegetables', 'Filipino', 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1),
('Pancit Bihon', 'Stir-fried rice noodles', 'Filipino', 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Lumpia Shanghai', 'Deep-fried spring rolls with meat filling', 'Filipino', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Fresh Lumpia', 'Fresh spring rolls with vegetables', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Sisig', 'Sizzling chopped pork face and ears', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Bangus Sisig', 'Sizzling chopped milkfish', 'Filipino', 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Chicken Inasal', 'Grilled chicken marinated in annatto', 'Filipino', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Pork BBQ', 'Filipino-style grilled pork skewers', 'Filipino', 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Chicken BBQ', 'Grilled chicken skewers', 'Filipino', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Liempo', 'Grilled pork belly', 'Filipino', 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Chicharon', 'Deep-fried pork rinds', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Dinuguan', 'Pork blood stew', 'Filipino', 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Menudo', 'Pork and liver stew', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Caldereta', 'Goat or beef stew in tomato sauce', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0),
('Mechado', 'Beef stew with tomato sauce', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Afritada', 'Chicken or pork stew with vegetables', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Bicol Express', 'Spicy pork in coconut milk', 'Filipino', 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Laing', 'Taro leaves in coconut milk', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1),
('Pinakbet', 'Mixed vegetables with shrimp paste', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 1, 1),
('Ginataang Langka', 'Young jackfruit in coconut milk', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Halo-Halo', 'Mixed shaved ice dessert', 'Filipino', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Leche Flan', 'Caramel custard dessert', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Buko Pie', 'Young coconut pie', 'Filipino', 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Bibingka', 'Rice cake with cheese and salted egg', 'Filipino', 0, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Puto', 'Steamed rice cake', 'Filipino', 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Kutsinta', 'Brown rice cake with coconut', 'Filipino', 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Taho', 'Soft tofu with syrup and tapioca pearls', 'Filipino', 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Balut', 'Duck embryo delicacy', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Kwek-Kwek', 'Deep-fried quail eggs in orange batter', 'Filipino', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0),
('Fishball', 'Deep-fried fish balls', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Kikiam', 'Chinese-Filipino sausage rolls', 'Filipino', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Isaw', 'Grilled chicken intestines', 'Filipino', 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Betamax', 'Grilled chicken blood cubes', 'Filipino', 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),

-- Japanese Cuisine (Popular in Davao)
('Sushi', 'Japanese raw fish with seasoned rice', 'Japanese', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Sashimi', 'Fresh raw fish slices', 'Japanese', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Ramen', 'Japanese noodle soup', 'Japanese', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Tempura', 'Battered and fried seafood or vegetables', 'Japanese', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Teriyaki Chicken', 'Grilled chicken with sweet soy glaze', 'Japanese', 1, 1, 1, 1, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0),
('Chicken Katsu', 'Breaded and fried chicken cutlet', 'Japanese', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Pork Katsu', 'Breaded and fried pork cutlet', 'Japanese', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('California Maki', 'Sushi roll with crab and avocado', 'Japanese', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Salmon Maki', 'Sushi roll with salmon', 'Japanese', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Tuna Maki', 'Sushi roll with tuna', 'Japanese', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Yakisoba', 'Stir-fried Japanese noodles', 'Japanese', 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1),
('Gyoza', 'Pan-fried pork dumplings', 'Japanese', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Miso Soup', 'Soybean paste soup', 'Japanese', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 1),
('Chicken Teriyaki', 'Grilled chicken with teriyaki sauce', 'Japanese', 1, 1, 1, 1, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0),
('Beef Teriyaki', 'Grilled beef with teriyaki sauce', 'Japanese', 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 1, 0, 0),

-- Korean Cuisine (Growing popularity in Davao)
('Korean BBQ', 'Grilled marinated Korean meat', 'Korean', 1, 1, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0),
('Bulgogi', 'Marinated grilled beef', 'Korean', 1, 1, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0),
('Galbi', 'Marinated grilled short ribs', 'Korean', 1, 1, 1, 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0),
('Kimchi', 'Fermented spicy cabbage', 'Korean', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 1, 1),
('Bibimbap', 'Mixed rice bowl with vegetables', 'Korean', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Kimchi Fried Rice', 'Fried rice with kimchi', 'Korean', 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Korean Fried Chicken', 'Crispy Korean-style fried chicken', 'Korean', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Japchae', 'Stir-fried sweet potato noodles', 'Korean', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Tteokbokki', 'Spicy rice cakes', 'Korean', 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Korean Ramyeon', 'Korean instant noodles', 'Korean', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),

-- Chinese Cuisine
('Fried Rice', 'Stir-fried rice with vegetables and meat', 'Chinese', 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Yang Chow Fried Rice', 'Special fried rice with shrimp and Chinese sausage', 'Chinese', 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Sweet and Sour Pork', 'Battered pork in sweet and sour sauce', 'Chinese', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Sweet and Sour Chicken', 'Battered chicken in sweet and sour sauce', 'Chinese', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Chow Mein', 'Stir-fried noodles with vegetables', 'Chinese', 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1),
('Lo Mein', 'Soft noodles with sauce and vegetables', 'Chinese', 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1),
('Dumplings', 'Steamed or fried dough parcels', 'Chinese', 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Siomai', 'Steamed pork dumplings', 'Chinese', 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Xiao Long Bao', 'Soup dumplings', 'Chinese', 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Spring Rolls', 'Crispy vegetable rolls', 'Chinese', 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Wonton Soup', 'Soup with meat-filled dumplings', 'Chinese', 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Hot and Sour Soup', 'Spicy and tangy soup', 'Chinese', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Mapo Tofu', 'Spicy tofu in Sichuan sauce', 'Chinese', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 1),
('Kung Pao Chicken', 'Spicy diced chicken with peanuts', 'Chinese', 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0),
('General Tso Chicken', 'Sweet and spicy fried chicken', 'Chinese', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Orange Chicken', 'Battered chicken in orange sauce', 'Chinese', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Beef and Broccoli', 'Stir-fried beef with broccoli', 'Chinese', 1, 1, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0),
('Honey Walnut Shrimp', 'Fried shrimp with candied walnuts', 'Chinese', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),

-- Italian Cuisine
('Pizza Margherita', 'Classic pizza with tomato, mozzarella, and basil', 'Italian', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Pepperoni Pizza', 'Pizza with pepperoni slices', 'Italian', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Hawaiian Pizza', 'Pizza with ham and pineapple', 'Italian', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Meat Lovers Pizza', 'Pizza with multiple meat toppings', 'Italian', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Carbonara', 'Pasta with eggs, cheese, and bacon', 'Italian', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Bolognese', 'Pasta with meat sauce', 'Italian', 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 0, 0, 1, 0, 0),
('Alfredo', 'Pasta with cream and cheese sauce', 'Italian', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Aglio Olio', 'Pasta with garlic and olive oil', 'Italian', 1, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Puttanesca', 'Pasta with tomatoes, olives, and anchovies', 'Italian', 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Lasagna', 'Layered pasta with meat and cheese', 'Italian', 0, 0, 1, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 0, 0),
('Ravioli', 'Stuffed pasta parcels', 'Italian', 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Gnocchi', 'Potato dumplings with sauce', 'Italian', 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 0, 0, 1, 0),
('Risotto', 'Creamy Italian rice dish', 'Italian', 1, 1, 0, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0),
('Minestrone Soup', 'Italian vegetable soup', 'Italian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Caesar Salad', 'Lettuce with Caesar dressing and croutons', 'Italian', 1, 1, 0, 1, 1, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0),
('Caprese Salad', 'Tomato, mozzarella, and basil salad', 'Italian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Tiramisu', 'Italian coffee-flavored dessert', 'Italian', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Gelato', 'Italian ice cream', 'Italian', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),

-- American/Western Cuisine
('Burger', 'Grilled beef patty in a bun', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 0),
('Cheeseburger', 'Burger with cheese', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1, 0, 0),
('Bacon Burger', 'Burger with bacon', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Chicken Burger', 'Grilled chicken breast in a bun', 'American', 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0),
('Fish Burger', 'Fried fish fillet in a bun', 'American', 1, 1, 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0, 0),
('French Fries', 'Deep-fried potato strips', 'American', 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Onion Rings', 'Battered and fried onion rings', 'American', 0, 0, 0, 0, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Chicken Wings', 'Fried or grilled chicken wings', 'American', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Buffalo Wings', 'Spicy chicken wings with buffalo sauce', 'American', 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Fried Chicken', 'Crispy fried chicken pieces', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Grilled Chicken', 'Grilled chicken breast or thighs', 'American', 1, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('BBQ Ribs', 'Barbecued pork ribs', 'American', 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Pulled Pork', 'Slow-cooked shredded pork', 'American', 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Steak', 'Grilled beef steak', 'American', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('T-Bone Steak', 'T-shaped beef steak', 'American', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Ribeye Steak', 'Marbled beef steak', 'American', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Fish and Chips', 'Battered fish with fried potatoes', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Mac and Cheese', 'Macaroni pasta with cheese sauce', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Meatloaf', 'Seasoned ground meat shaped into a loaf', 'American', 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0),
('Meatballs', 'Seasoned ground meat balls', 'American', 1, 1, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0),
('Hot Dog', 'Grilled sausage in a bun', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Corn Dog', 'Battered and fried hot dog on a stick', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Pancakes', 'Fluffy breakfast cakes with syrup', 'American', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Waffles', 'Grid-patterned breakfast cakes', 'American', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Eggs Benedict', 'Poached eggs on English muffin with hollandaise', 'American', 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Club Sandwich', 'Multi-layered sandwich with chicken and bacon', 'American', 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('BLT Sandwich', 'Bacon, lettuce, and tomato sandwich', 'American', 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Grilled Cheese', 'Grilled sandwich with melted cheese', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),

-- Mexican Cuisine
('Tacos', 'Folded tortillas with meat and vegetables', 'Mexican', 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Burritos', 'Large wrapped tortillas with fillings', 'Mexican', 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Quesadillas', 'Grilled tortillas with melted cheese', 'Mexican', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Nachos', 'Tortilla chips with cheese and toppings', 'Mexican', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Enchiladas', 'Rolled tortillas with sauce and cheese', 'Mexican', 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Fajitas', 'Grilled meat with peppers and onions', 'Mexican', 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0),
('Guacamole', 'Avocado dip with lime and spices', 'Mexican', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Salsa', 'Tomato-based dip with chilies', 'Mexican', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Churros', 'Fried dough pastry with cinnamon sugar', 'Mexican', 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 1),
('Tres Leches', 'Three milk sponge cake', 'Mexican', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),

-- Thai Cuisine
('Pad Thai', 'Stir-fried noodles with tamarind sauce', 'Thai', 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 0, 1, 0, 0),
('Tom Yum', 'Spicy and sour Thai soup', 'Thai', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Tom Kha', 'Coconut chicken soup', 'Thai', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Green Curry', 'Spicy green curry with coconut milk', 'Thai', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Red Curry', 'Spicy red curry with coconut milk', 'Thai', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Massaman Curry', 'Rich and mild Thai curry', 'Thai', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 1, 0, 0),
('Pad Krapow', 'Stir-fried basil with meat', 'Thai', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Mango Sticky Rice', 'Sweet dessert with coconut sauce', 'Thai', 1, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Thai Fried Rice', 'Fragrant fried rice with Thai flavors', 'Thai', 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Som Tam', 'Spicy green papaya salad', 'Thai', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1),

-- Vietnamese Cuisine
('Pho', 'Vietnamese noodle soup with herbs', 'Vietnamese', 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 0),
('Banh Mi', 'Vietnamese baguette sandwich', 'Vietnamese', 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Spring Rolls', 'Fresh rice paper rolls', 'Vietnamese', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1),
('Vietnamese Fried Rice', 'Fragrant fried rice', 'Vietnamese', 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Bun Bo Hue', 'Spicy beef noodle soup', 'Vietnamese', 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0, 1, 0, 0),

-- Indian Cuisine
('Chicken Curry', 'Spiced chicken in sauce', 'Indian', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Beef Curry', 'Spiced beef in sauce', 'Indian', 1, 1, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0),
('Vegetable Curry', 'Mixed vegetables in spiced sauce', 'Indian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Chicken Tikka Masala', 'Chicken in creamy tomato sauce', 'Indian', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0),
('Butter Chicken', 'Mild chicken curry in butter sauce', 'Indian', 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0),
('Biryani', 'Fragrant spiced rice with meat', 'Indian', 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Tandoori Chicken', 'Clay oven-roasted spiced chicken', 'Indian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Naan Bread', 'Indian flatbread', 'Indian', 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1, 0, 0, 1, 0),
('Roti', 'Whole wheat flatbread', 'Indian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Samosa', 'Fried pastry with spiced filling', 'Indian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Dal', 'Lentil curry', 'Indian', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1),
('Palak Paneer', 'Spinach with Indian cottage cheese', 'Indian', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 1, 0),

-- Seafood (Davao specialty)
('Grilled Tuna', 'Fresh grilled tuna steak', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Grilled Salmon', 'Grilled salmon fillet', 'International', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Grilled Bangus', 'Grilled milkfish', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Grilled Tilapia', 'Grilled freshwater fish', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Fried Pompano', 'Deep-fried white fish', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0),
('Steamed Lapu-Lapu', 'Steamed grouper fish', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Grilled Squid', 'Grilled squid with soy sauce', 'Filipino', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0),
('Grilled Prawns', 'Large grilled shrimp', 'Filipino', 1, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Shrimp Tempura', 'Battered and fried shrimp', 'Japanese', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Crab in Coconut Milk', 'Crab cooked in coconut sauce', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Butter Garlic Shrimp', 'Shrimp in butter and garlic', 'International', 1, 1, 1, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0),
('Salt and Pepper Fish', 'Crispy fish with salt and pepper', 'Chinese', 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0),

-- Healthy Options
('Quinoa Bowl', 'Nutritious grain bowl with vegetables', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1),
('Chicken Salad', 'Fresh salad with grilled chicken', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0),
('Tuna Salad', 'Fresh salad with tuna', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Greek Salad', 'Mediterranean salad with feta cheese', 'Greek', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Fruit Salad', 'Mixed fresh fruits', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Vegetable Salad', 'Mixed fresh vegetables', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Grilled Vegetables', 'Assorted grilled vegetables', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Steamed Vegetables', 'Healthy steamed mixed vegetables', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Vegetable Stir Fry', 'Quick-cooked mixed vegetables', 'Asian', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Tofu Stir Fry', 'Stir-fried tofu with vegetables', 'Asian', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Avocado Toast', 'Toasted bread with avocado spread', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Smoothie Bowl', 'Thick smoothie topped with fruits', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),

-- Breakfast Items
('Tapsilog', 'Filipino beef tapa with rice and egg', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Longsilog', 'Filipino sausage with rice and egg', 'Filipino', 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Bangsilog', 'Milkfish with rice and egg', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0),
('Cornsilog', 'Corned beef with rice and egg', 'Filipino', 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0),
('Hotsilog', 'Hot dog with rice and egg', 'Filipino', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0),
('Omelet', 'Beaten eggs cooked with fillings', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0),
('Scrambled Eggs', 'Softly cooked beaten eggs', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0),
('Fried Eggs', 'Eggs cooked sunny-side up or over easy', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 0),
('Bacon and Eggs', 'Classic breakfast combination', 'American', 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('Ham and Eggs', 'Ham served with eggs', 'American', 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0),
('French Toast', 'Bread soaked in egg and fried', 'French', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Cereals', 'Breakfast cereal with milk', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Oatmeal', 'Cooked oats with toppings', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1),

-- Snacks and Street Food
('Banana Cue', 'Deep-fried banana with brown sugar', 'Filipino', 0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Camote Cue', 'Deep-fried sweet potato with brown sugar', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Turon', 'Deep-fried banana spring roll', 'Filipino', 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 1),
('Biko', 'Sweet sticky rice cake', 'Filipino', 0, 1, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Suman', 'Rice cake wrapped in banana leaves', 'Filipino', 1, 1, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Mais Con Yelo', 'Corn with shaved ice and milk', 'Filipino', 0, 1, 0, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Dirty Ice Cream', 'Local ice cream in various flavors', 'Filipino', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Popcorn', 'Popped corn kernels', 'International', 1, 1, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Potato Chips', 'Thin-sliced fried potatoes', 'International', 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Crackers', 'Crispy baked snack crackers', 'International', 0, 1, 1, 0, 0, 1, 1, 1, 0, 1, 0, 0, 0, 1, 1),

-- Beverages (Food-related) - Note: beverages marked as food items for algorithm
('Fresh Fruit Shake', 'Blended fresh fruits with ice', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Mango Shake', 'Blended mango with milk and ice', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Durian Shake', 'Blended durian with milk', 'Filipino', 1, 0, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Avocado Shake', 'Creamy avocado smoothie', 'Filipino', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0),
('Buko Juice', 'Fresh coconut water', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Calamansi Juice', 'Local lime juice', 'Filipino', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Lemonade', 'Fresh lemon juice with water', 'International', 1, 1, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Iced Tea', 'Cold brewed tea', 'International', 1, 1, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Coffee', 'Hot brewed coffee', 'International', 1, 1, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Iced Coffee', 'Cold coffee drink', 'International', 1, 1, 0, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Hot Chocolate', 'Hot cocoa drink', 'International', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),

-- Desserts
('Ice Cream', 'Frozen dairy dessert', 'International', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Cake', 'Sweet baked dessert', 'International', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Chocolate Cake', 'Rich chocolate-flavored cake', 'International', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Cheesecake', 'Cream cheese-based cake', 'International', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Brownies', 'Dense chocolate squares', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 1, 0),
('Cookies', 'Sweet baked treats', 'International', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Donuts', 'Fried dough rings with glaze', 'American', 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Pastries', 'Sweet baked goods', 'International', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Pie', 'Pastry with sweet or savory filling', 'International', 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0),
('Pudding', 'Creamy dessert', 'International', 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0),
('Jelly', 'Gelatin-based dessert', 'International', 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1),
('Candy', 'Sweet confectionery', 'International', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1),
('Chocolate', 'Sweet cocoa-based treat', 'International', 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0);

-- Verify the insert
SELECT COUNT(*) as 'Total Food Types Inserted' FROM FoodTypes;

-- Display sample of inserted food types with their nutritional properties
SELECT TOP 20 
    FoodTypeId, 
    Name, 
    CuisineType,
    IsHealthy,
    IsLowCalorie,
    IsHighProtein,
    IsVegetarian,
    IsVegan
FROM FoodTypes 
ORDER BY Name;

-- Display healthy food options
SELECT COUNT(*) as 'Healthy Food Options' FROM FoodTypes WHERE IsHealthy = 1;
SELECT COUNT(*) as 'Vegetarian Options' FROM FoodTypes WHERE IsVegetarian = 1;
SELECT COUNT(*) as 'Vegan Options' FROM FoodTypes WHERE IsVegan = 1;
SELECT COUNT(*) as 'High Protein Options' FROM FoodTypes WHERE IsHighProtein = 1;
SELECT COUNT(*) as 'Low Calorie Options' FROM FoodTypes WHERE IsLowCalorie = 1;

PRINT 'Successfully inserted comprehensive Davao City food types with complete nutritional properties!';
PRINT 'All evidence-based algorithm fields populated for accurate recommendations.';
PRINT 'Ready for prescriptive food ranking system!';
