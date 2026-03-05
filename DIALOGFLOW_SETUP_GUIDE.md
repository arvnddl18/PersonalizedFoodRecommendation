# Dialogflow Setup Guide for Personalized Food Recommendations

## 🎯 New Intent: `personalized_food_recommendation`

### **Intent Purpose**
This intent handles generic food recommendation requests like:
- "Can you recommend something?"
- "I'm hungry"
- "What should I eat?"
- "Give me some suggestions"
- "I need food ideas"

### **Training Phrases**
Add these training phrases to the intent:

#### **Basic Requests**
- Can you recommend something?
- I'm hungry
- What should I eat?
- Give me some suggestions
- I need food ideas
- What do you recommend?
- Suggest something for me
- I want to eat something
- Help me choose food
- What's good to eat?

#### **Contextual Requests**
- I'm hungry, what do you suggest?
- Can you recommend something good?
- What should I eat today?
- Give me some food options
- I need a meal recommendation
- What's recommended for me?
- Suggest something tasty
- I want to try something new
- What would you recommend?
- Help me pick something to eat

### **Intent Configuration**

#### **1. Intent Name**
```
personalized_food_recommendation
```

#### **2. Action**
```
personalized_recommendation
```

#### **3. Parameters**
No specific parameters needed - this intent relies on user profile data and behavioral patterns.

#### **4. Responses**
The intent will trigger the backend prescriptive recommender engine, so no static responses are needed.

### **Intent Priority**
Set this intent to **HIGH PRIORITY** since it's a core user interaction.

### **Fallback Intent**
Ensure this intent is NOT marked as a fallback intent.

---

## 🔄 System Flow

### **1. User Input**
```
User: "Can you recommend something?"
```

### **2. Intent Detection**
```
Dialogflow detects: personalized_food_recommendation
```

### **3. Backend Processing**
```
- Calls prescriptive recommender engine
- Analyzes user profile and behavioral patterns
- Generates personalized recommendations
- Calculates weighted scores using the documented formula
```

### **4. Response Generation**
```
- Builds personalized context from user preferences
- Includes top recommendation with score and reasoning
- Provides actionable food suggestions
```

---

## 📊 Expected Response Format

### **Before (Generic Response)**
```
"I know you prefer vegetarian options and you love Sushi I've noticed you often enjoy american cuisine and you've shown interest in japanese, mexican, indian preferring budget options. Sit tight while I find some amazing options just for you!"
```

### **After (Prescriptive Response)**
```
"I know you prefer vegetarian options and you love Sushi I've noticed you often enjoy american cuisine and you've shown interest in japanese, mexican, indian preferring budget options. Based on your preferences, I recommend Mama's Italian Kitchen (Score: 0.87) Reason: aligns with your frequent Italian choices, fits your dinner context, supports your healthy goal. I've found some great recommendations for you!"
```

---

## 🎯 Implementation Benefits

### **1. 100% Prescriptive Algorithm Usage**
- Every personalized query now triggers the prescriptive recommender
- No more generic responses without actual recommendations
- Full implementation of the documented scoring formula

### **2. Enhanced User Experience**
- Users get actual food recommendations, not just descriptions
- Personalized context with specific establishment suggestions
- Transparent scoring and reasoning for recommendations

### **3. Better Behavioral Learning**
- All interactions are tracked with proper intent classification
- Consistent parameter storage for learning algorithms
- Improved pattern recognition from user queries

---

## 🚀 Next Steps

### **1. Dialogflow Setup**
- Create the new intent in your Dialogflow console
- Add all training phrases
- Set appropriate priority and configuration

### **2. Testing**
- Test with various generic food requests
- Verify prescriptive recommendations are generated
- Check that behavioral tracking works correctly

### **3. Monitoring**
- Monitor user satisfaction with personalized responses
- Track recommendation accuracy and relevance
- Analyze behavioral learning improvements

---

## 📝 Technical Notes

### **Intent Detection**
The system now properly detects generic food requests and routes them to the prescriptive recommender instead of generating generic responses.

### **Parameter Tracking**
All interactions are tracked with:
- Intent: `personalized_food_recommendation`
- Action: `search`
- Complete parameter set (even empty values)

### **Algorithm Implementation**
The prescriptive recommender now runs for 100% of personalized queries, ensuring the documented formula is fully utilized.
